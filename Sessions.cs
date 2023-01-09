
using Newtonsoft.Json.Linq;
using NLP.Models;
using System.Data;

namespace NLP
{
    public class Sessions
    {
        public static Models.Session Open(string agent_id, string origin_id, string origin)
        {
            //Trace.Message($"Session Opening... {agent_id} {origin_id} {origin}");
            NLP.Models.Session? session = null;

            DataSet ds = MySQL.Data.Query("SELECT uuid, agent_id, origin_id, origin, date_creation, expected FROM nlp_sessions WHERE agent_id=?agent_id AND origin_id=?origin_id AND origin=?origin AND closed<>1", new string[] { agent_id, origin_id, origin });
            //Trace.Message($"Session Opening Answer: {ds.Tables[0].Rows.Count}");
            if (ds.Tables[0].Rows.Count > 0)
            {
                session = new Session();
                session.uuid = ds.Tables[0].Rows[0]["uuid"].ToString();
                //Trace.Message($"uuid: {session.uuid}");
                session.agent_id = ds.Tables[0].Rows[0]["agent_id"].ToString();
                session.origin_id = ds.Tables[0].Rows[0]["origin_id"].ToString();
                session.messages = MySQL.Json.Select.Fill(MySQL.Data.Query("SELECT origin, phrase, date_creation FROM nlp_sessions_messages WHERE uuid=?uuid", new string[] { session.uuid })).Multiple<Message>().ToList();

                //Trace.Message($"expected id: {ds.Tables[0].Rows[0]["expected"].ToString()}");
                if (!String.IsNullOrEmpty(ds.Tables[0].Rows[0]["expected"].ToString()) && ds.Tables[0].Rows[0]["expected"].ToString() != "0")
                {
                    session.expect = Answer.Instance(Convert.ToInt32(ds.Tables[0].Rows[0]["expected"]));
                    //Trace.Message($"Session Expect... {session.expect.answer_id}");
                }
            }
            else
            {
                session = null;
            }

            if (session == null)
            {
                session = new NLP.Models.Session() { uuid = Guid.NewGuid().ToString(), agent_id = agent_id, origin_id = origin_id, origin = origin, messages = new List<Message>(), data = new List<Models.FormData>(), date_creation = DateTime.Now, date_update = DateTime.Now };
                MySQL.Json.Insert.Into(JObject.FromObject(new { uuid = session.uuid, agent_id = session.agent_id, origin_id = origin_id, origin = session.origin, date_creation = session.date_creation.ToString("yyyy-MM-dd HH:mm:ss") }), "nlp_sessions").Run();
                //Trace.Message($"Session Create... {session.uuid}");
            }
            else
            {
                session.date_update = DateTime.Now;
            }
            return session;
        }


        public static void Close(NLP.Models.Session session)
        {
           if(!String.IsNullOrEmpty(session.uuid)) Close(session.uuid);
        }

        public static void Close(string uuid)
        {
            MySQL.Data.Query("UPDATE nlp_sessions SET closed=1 WHERE uuid=?uuid", new string[] { uuid });
        }
    }
}
