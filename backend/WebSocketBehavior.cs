using System.Timers;
using Serilog;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Backend
{

    public class WebSocket : WebSocketBehavior
    {
        private static new readonly ILogger Log = Serilog.Log.ForContext<WebSocket>();
        private System.Timers.Timer messageTimer = new System.Timers.Timer(100); // 100ms timer
        private bool gotOffer = false;
        private bool gotAnswer = false;
        private Client client;
        
        public WebSocket()
        {
            messageTimer.Elapsed += this.OnTimedEvent;
            messageTimer.AutoReset = false; // Prevent the timer from restarting automatically
        }

        public void PublicSend(string data)
        {
            if (Context.WebSocket.IsAlive)
            {
                Send(data);
            }
        }

        protected override void OnOpen()
        {
            Log.Debug("WebSocket {id} connection opened", ID);
            client = new Client()
            {
                ID = ID,
                socket = this
            };

            Lobby.AddClient(this.client);

        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Log.Debug($"{this.ID} sent: {e.Data}");

            if (!gotOffer)
            {
                gotOffer = (e.Data.Contains("\"type\":\"offer\""));
            }
            
            if (!gotAnswer)
            {
                gotAnswer = (e.Data.Contains("\"type\":\"answer\""));
            }


            if (!e.Data.Contains("cmd:") && !e.Data.Contains("dbg:"))
            {
                client.curRoom.SendToRoom(this.ID, e.Data);
                messageTimer.Stop();
                messageTimer.Start();
            }
            else if (e.Data.Contains("cmd:disconnect"))
            {
                if(this.client.curRoom != null)
                {
                    var opp = this.client.currOpp;

                    this.client.curRoom.RemoveClient(this.client);

                    Lobby.MatchClient(this.client);
                    Lobby.MatchClient(opp);
                }

            }
            else if (e.Data.Contains("cmd:connect"))
            {
                client.Ready = true;
                Lobby.MatchClient(this.client);
            }
            if (e.Data.Contains("dbg: "))
            {
                string modifiedString = e.Data.Replace("dbg: ", "");
                Log.Debug(modifiedString);
            }
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            messageTimer.Stop();

            if (client.curRoom.sockMessage != null)
            {
                Log.Debug($"{client.currOpp.ID} received {client.curRoom.sockMessage.ToString()}");
                client.curRoom.SendToClient(client.currOpp, client.curRoom.sockMessage.ToString());
                client.curRoom.sockMessage = null;
            }

            if (gotOffer)
            {
                gotOffer = false;
                Log.Debug($"{client.currOpp.ID} revieved offer");
                client.curRoom.SendCommandToClient(client.currOpp, "create_answer");
            }
            else if (gotAnswer)
            {
                gotAnswer = false;
                Log.Debug($"{this.ID} sent add_answer");
                client.curRoom.SendCommandToClient(client.currOpp, "add_answer");
            }
        }


        protected override void OnClose(CloseEventArgs e)
        {
            Lobby.RemoveClient(this.client);
            Log.Debug($"Client {this.client.ID} disconnected");
        }

    }

}
