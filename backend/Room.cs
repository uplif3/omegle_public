using Newtonsoft.Json;
using Serilog;

namespace Backend
{
    public class CommandMsg
    {
        public string type;
        public string data;
    }

    public class Room
    {
        private readonly ILogger Log = Serilog.Log.ForContext<Room>();

        public List<Client> clients = new List<Client>();
        public Client clientOne;

        private Dictionary<string, List<Object>> buffers = new Dictionary<string, List<object>>();
        public Object sockMessage;
        public bool FirstIsOffer;

        public void AddClientToRoom(Client client)
        {
            if (clients.Count > 2) return;

            if (clients.Count == 0)
            {
                clientOne = client;
            }
            clients.Add(client);
            Log.Debug($"Client {client.ID} joined the room");

            if (clients.Count == 2)
            {
                
                StartSignalingProcess();
            }
        }

        public void RemoveClient(Client client)
        {
            Log.Debug("Client {ClientId} left the room", client.ID);
            Log.Debug("Client {ClientId} was removed from the room", client.currOpp.ID);
            clients.Remove(client);
            clients.Remove(client.currOpp);

            client.currOpp.currOpp = null;
            client.currOpp.Ready = true;
            client.currOpp.curRoom = null;

            client.currOpp = null;
            client.Ready = true;
            client.curRoom = null;

            Lobby.DeleteRoom(this);
        }

        private void StartSignalingProcess()
        {
            var clientTwo = GetOtherClient(clientOne);
            clientOne.currOpp = clientTwo;
            clientTwo.currOpp = clientOne;

            SendCommandToClient(clientOne, "initiate_offer");
        }

        public void SendCommandToClient(Client client, string command)
        {
            var message = new CommandMsg
            {
                type = "command",
                data = command
            };

            var jsonMessage = JsonConvert.SerializeObject(message);
            client.socket.PublicSend(jsonMessage);
            Log.Debug($"Sent {command} to client {client.ID}");
        }

        public void SendToClient(Client client, string command)
        {
            client.socket.PublicSend(command);

            Log.Debug($"Sent {command} to client {client.ID}");
        }

        public void SendToRoom(string clientId, object msg)
        {
            var msg_string = msg.ToString();
            if (msg.ToString().Contains("\"type\":\"offer\""))
            {
                Log.Debug("send first added");
                sockMessage = msg;
                FirstIsOffer = true;
            }
            else if (msg.ToString().Contains("\"type\":\"answer\""))
            {
                Log.Debug("send first added");
                sockMessage = msg;
                FirstIsOffer = false;
            }
        }

        public Client GetOtherClient(Client currentClient)
        {
            return clients.FirstOrDefault(client => client != currentClient);
        }

    }
}
