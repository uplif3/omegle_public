
let creatOfferBtn = document.getElementById("createOffer");
let creatAnswerBtn = document.getElementById("createAnswer");
let scrolltop = document.getElementById("scroll_top_btn");
let peerConnection;
let offer;

const numAudioTracksInput = document.getElementById("numAudioTracksInput");
const numAudioTracksDisplay = document.getElementById("numAudioTracksDisplay");

window.onload = () => {

    creatOfferBtn.addEventListener("click", createOffer);
    creatAnswerBtn.addEventListener("click", createAnswer);

    numAudioTracksInput.addEventListener('change', e => numAudioTracksDisplay.innerText = e.target.value);

    scrolltop.addEventListener("click", topFunction);
};


async function createOffer() {
    peerConnection = new RTCPeerConnection();

    const numRequestedAudioTracks = parseInt(numAudioTracksInput.value);
    for (let i = 0; i < numRequestedAudioTracks; i++) {
        const acx = new AudioContext();
        const dst = acx.createMediaStreamDestination();
    
        // Fill up the peer connection with numRequestedAudioTracks number of tracks.
        const track = dst.stream.getTracks()[0];
        peerConnection.addTrack(track, dst.stream);
    }

    offer = await peerConnection.createOffer();
    await peerConnection.setLocalDescription(offer);
    document.getElementById("sdp-offer").value = offer.sdp;
}


async function createAnswer() {
    if(!offer) {
        return alert("Zuerst muss ein Offer erstellt werden.");
    }

    await peerConnection.setRemoteDescription(offer);
    
    let answer = await peerConnection.createAnswer();
    await peerConnection.setLocalDescription(answer);

    document.getElementById("sdp-answer").value = answer.sdp;
}


// When the user scrolls down 20px from the top of the document, show scroll to top button
window.onscroll = function() {scrollFunction()};

function scrollFunction() {
  if (window.screen.width >= 600 && (document.body.scrollTop > 20 || document.documentElement.scrollTop > 20)) {
    scrolltop.style.display = "block";
  } else {
    scrolltop.style.display = "none";
  }
}

// When the user clicks on the button, scroll to the top of the document
function topFunction() {
  document.documentElement.scrollTop = 0;
} 