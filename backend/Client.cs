using Serilog;

namespace Backend
{
    public class Client
    {
        public string ID;
        public WebSocket socket;
        public Client? currOpp;
        public List<Client> previousOpps = new List<Client>();
        public bool Ready = false;
        public bool Connected = false;
        public Room curRoom;

        public bool wasOpp(Client opp)
        {
            if (previousOpps.Count > 0)
            {
                foreach (var prev in previousOpps)
                {
                    if (opp == prev)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
