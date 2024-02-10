const media_constraints = {
    video: {
      width: {
        min: 640,
        max: 2560,
      },
      height: {
        min: 360,
        max: 1440
      },
    },
    audio: true
};

const server_configuration = {
    iceServers:[
        {
            urls:["stun:stun.l.google.com:19302", "stun:stun.2.google.com:19302"] // Ein STUN-Server für die NAT-Traversal
        }
    ]
};

let websock;
let peerConnection;
let localStream;
let remoteStream;

// save offer and answer for both clients
let offerSdp;
let answerSdp;

var chatChannel; // textchat

window.onload = () => {

    if (hasGetUserMedia()) {
        // Browser supported :)
        try {
            websock = new WebSocket("wss://" + window.location.hostname + ":8080/signaling");
        }
        catch (err) {
            alert("No connection to signalling server!");
        }
        if (websock != undefined)
        {
            initDevices();
            document.getElementById("start-stream").disabled = false;
            initEventListeners();
        }
    } else {
        alert("getUserMedia() is not supported in your browser");
    }

};

let initDevices = async () => {
    localStream = await navigator.mediaDevices.getUserMedia(media_constraints).catch((err) => {
        alert("Could not find a camera, please check if it is turned on!\nOtherwise please check permissions!");
    });

    document.getElementById("local-client").srcObject = localStream;
}


function initEventListeners() {

    // Buttons
    document.getElementById("start-stream").addEventListener("click", function() {
        sendToServer("cmd:connect");
        document.getElementById("start-stream").disabled = true;
        document.getElementById("start-stream").hidden = true;
        document.getElementById("new-connection").hidden = false;
        document.getElementById("new-connection").disabled = false;
        document.getElementById("remoteloader").hidden = false;
        debugBackend("client clicked start-stream");
    });

    document.getElementById("new-connection").addEventListener("click", function() {
        document.getElementById("remote-client").srcObject.getTracks().forEach(track => track.stop());
        chatChannel.close();
        peerConnection.close()
        peerConnection = null;
        remoteStream = null;
        localStream.getTracks().forEach(track => track.stop()); // not automatically closed on peerConnection.close()
        localStream = null;
        initDevices();  // reset local stream to show webcam image
        offerSdp = null;
        answerSdp = null;
        chatChannel = null;
        sendToServer("cmd:disconnect");
        document.getElementById("chatbox").replaceChildren();
        document.getElementById("remoteloader").hidden = false;
        document.getElementById("sendchat").disabled = true;
        debugBackend("client clicked new-connection");
    });

    document.getElementById("sendchat").addEventListener("click", function(event) {
        // event.preventDefault();
        debugBackend("client clicked send button");
        sendText();
    });
    document.getElementById("chatinput").addEventListener("keypress", function(event) {
        // event.preventDefault();
        if (event.key === "Enter") {
            debugBackend("client pressed enter on chat");
            sendText();
        }
    });

    // Communication
    websock.addEventListener("message", (event) => {
        handleMessageFromServer(event);
    });

}

function debugBackend(message) {
    sendToServer("dbg: " + message);
}

function sendToServer(message) {
    if (websock != undefined) {
        websock.send(message);
    }
    else {
        console.log("Tried sending to server, without websocket connection");
    }
}

function hasGetUserMedia() {
    return !!(navigator.getUserMedia || navigator.webkitGetUserMedia ||
            navigator.mozGetUserMedia || navigator.msGetUserMedia || navigator.mediaDevices.getUserMedia);
}


function addToChat(message, fromLocal=false) {
    let d = document.createElement("div");
    d.classList.add("chat-message");
    if (fromLocal) {
        d.innerHTML = "You: " + message;
    }
    else {
        d.innerHTML = "Them: " + message;
    }
    document.getElementById("chatbox").appendChild(d);
    document.getElementById("chatbox").scrollTop = document.getElementById("chatbox").scrollHeight; // scroll to bottom of chatbox
    debugBackend("client added text to chat: " + message);
}

function sendText() {
    let data = document.getElementById("chatinput").value;
    if (chatChannel && data.trim() !== "") {
        chatChannel.send(data);
        addToChat(data, true);
        document.getElementById("chatinput").value = "";
        debugBackend("client sent text: " + data);
    }
}


let handleMessageFromServer = async (message) => {
    const parsedMessage = JSON.parse(message.data);

    if (parsedMessage.type === "command") {
        if (parsedMessage.data === "initiate_offer") {
            if (!localStream) {
                await initDevices();
            }
            await createOffer();
        } else if (parsedMessage.data === "create_answer") {
            if (!localStream) {
                await initDevices();
            }
            await createAnswer();
        } else if (parsedMessage.data === "add_answer") {
            await addAnswer();
            sendToServer("cmd:Success Connection Established");
        }
    } else if (parsedMessage.type === "offer") {
        offerSdp = parsedMessage;
    }
    else if (parsedMessage.type === "answer") {
        answerSdp = parsedMessage;
    }
}


// client 1, start sending an offer and your icecandidates
let createOffer = async () => {
    peerConnection = new RTCPeerConnection(server_configuration)

    remoteStream = new MediaStream();
    document.getElementById("remote-client").srcObject = remoteStream;

    chatChannel = peerConnection.createDataChannel("chat");
    debugBackend("client who offered created datachannel");
    chatChannel.addEventListener("message", (event) => {
        debugBackend("client who offered received text: " + event.data);
        addToChat(event.data);
    })

    localStream.getTracks().forEach((track) => {
        peerConnection.addTrack(track, localStream);
    })

    peerConnection.ontrack = async (event) => {
        event.streams[0].getTracks().forEach((track) =>{
            remoteStream.addTrack(track);
        })
    }

    peerConnection.onicecandidate = async (event) => {
        if(event.candidate){
            stringedOffer = JSON.stringify(peerConnection.localDescription)
            sendToServer(stringedOffer);
        }
    }

    // check if remote is gone
    // peerConnection.onconnectionstatechange = () => {
    //     let connectionStatus = peerConnection.connectionState;
    //     if (["disconnected", "failed", "closed"].includes(connectionStatus)) {
    //         console.log("disconnected");
    //     }
    // };

    let offer = await peerConnection.createOffer();
    await peerConnection.setLocalDescription(offer);

};

// client 2, create answer to a received offer, send it and icecandidates
let createAnswer = async () => {
    peerConnection = new RTCPeerConnection(server_configuration)

    remoteStream = new MediaStream();
    document.getElementById("remote-client").srcObject = remoteStream;

    peerConnection.ondatachannel = (event) => {
        debugBackend("client who answered received datachannel");
        chatChannel = event.channel;
        chatChannel.addEventListener("message", (event) => {
            debugBackend("client who answered received text: " + event.data);
            addToChat(event.data);
        })
        chatChannel.onclose = () => {
            document.getElementById("remote-client").src = "";  // src not srcObject!
            document.getElementById("chatbox").replaceChildren();
        };
    };

    localStream.getTracks().forEach((track) => {
        peerConnection.addTrack(track, localStream);
    })

    peerConnection.ontrack = async (event) => {
        event.streams[0].getTracks().forEach((track) =>{
            remoteStream.addTrack(track);
        })
    }

    peerConnection.onicecandidate = async (event) => {
        if(event.candidate){
            stringedAnswer = JSON.stringify(peerConnection.localDescription);
            sendToServer(stringedAnswer);
        }
    }

    // check if remote is gone
    // peerConnection.onconnectionstatechange = () => {
    //     let connectionStatus = peerConnection.connectionState;
    //     if (["disconnected", "failed", "closed"].includes(connectionStatus)) {
    //         console.log("disconnected");
    //     }
    // };

    document.getElementById("remoteloader").hidden = true;
    document.getElementById("sendchat").disabled = false;

    let offer = offerSdp;
    if(!offer) {
        return alert("Retrieve offer from peer first!");
    }
    await peerConnection.setRemoteDescription(offer);

    let answer = await peerConnection.createAnswer()
    await peerConnection.setLocalDescription(answer)
};

// client 1, save the received answer, so both clients have all needed data
let addAnswer = async () => {
    let answer = answerSdp
    if(!answer) {
        return alert("Retrieve Answer from peer first...");
    }
    if (!peerConnection.currentRemoteDescription){
        peerConnection.setRemoteDescription(answer);
    }
    chatChannel.onclose = () => {
        document.getElementById("remote-client").src = "";  // src not srcObject!
        document.getElementById("chatbox").replaceChildren();
        // console.log("chat disconnected");
    };
    document.getElementById("sendchat").disabled = false;
    document.getElementById("remoteloader").hidden = true;
}
