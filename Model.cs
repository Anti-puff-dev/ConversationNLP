using MySQL;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Data;

namespace NLP
{
    public class Model
    {
        public static async Task ClearDb(int agent_id)
        {
            Console.WriteLine("Clearing Database");


            DataSet ds = Data.Query("SELECT intent_id FROM nlp_intents WHERE agent_id=?agent_id", new string[] { agent_id.ToString() });
            Console.WriteLine("Intents: " + ds.Tables[0].Rows.Count);
            if (ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    string intent_id = row[0].ToString();
                    MySQL.Json.Delete.From("nlp_questions", "intent_id", intent_id).Run();
                    MySQL.Json.Delete.From("nlp_answers", "intent_id", intent_id).Run();
                }
            }

            MySQL.Json.Delete.From("nlp_agents", "agent_id", agent_id.ToString()).Run();
            MySQL.Json.Delete.From("nlp_greetings", "agent_id", agent_id.ToString()).Run();
            MySQL.Json.Delete.From("nlp_intents", "agent_id", agent_id.ToString()).Run();
            MySQL.Json.Delete.From("nlp_dataset", "agent_id", agent_id.ToString()).Run();
        }


        public static async Task TruncateDb()
        {
            Console.WriteLine("Truncating Database");
            Data.Query("TRUNCATE TABLE nlp_agents;", new string[] { });
            Data.Query("TRUNCATE TABLE nlp_greetings;", new string[] { });
            Data.Query("TRUNCATE TABLE nlp_intents;", new string[] { });
            Data.Query("TRUNCATE TABLE nlp_questions;", new string[] { });
            Data.Query("TRUNCATE TABLE nlp_answers;", new string[] { });
            Data.Query("TRUNCATE TABLE nlp_dataset;", new string[] { });
        }


        public static async Task TrainDb(string agent_id)
        {
            Console.WriteLine("Truncating Dataset");
            //Data.Query("TRUNCATE TABLE nlp_dataset;", new string[] { });
            ClearDataset(agent_id);

            string intent_id = "";
            DataSet ds = Data.Query("SELECT intent_id, phrase FROM nlp_questions WHERE agent_id=?agent_id ORDER BY intent_id ASC", new string[] { agent_id });

            Hashtable hashtable = new Hashtable();

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                if (intent_id != dr[0].ToString())
                {
                    intent_id = dr[0].ToString();
                    hashtable[dr[0].ToString()] = new List<string>();
                }

                ((List<string>)hashtable[dr[0].ToString()]).Add(dr[1].ToString());
            }

            NLP.Classify.word_pooling = 0.7d;
            NLP.Classify.AgentId = agent_id;

            foreach (DictionaryEntry ht in hashtable)
            {
                NLP.Classify.TrainIntentGroup(((List<string>)hashtable[ht.Key.ToString()]).ToArray(), new string[] { ht.Key.ToString() }, true, 10);
            }

            Console.WriteLine("TrainDb Finished");
        }


        public static async Task TrainDb(string agent_id, string intent_id)
        {
            //Console.WriteLine("Truncating Dataset");
            //Data.Query("TRUNCATE TABLE nlp_dataset;", new string[] { });

            DataSet ds = Data.Query("SELECT intent_id, phrase FROM nlp_questions WHERE agent_id=?agent_id AND intent_id=?intent_id ORDER BY intent_id ASC", new string[] { agent_id, intent_id });

            Hashtable hashtable = new Hashtable();

            string _intent_id = "";
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                if (_intent_id != dr[0].ToString())
                {
                    _intent_id = dr[0].ToString();
                    hashtable[dr[0].ToString()] = new List<string>();
                }

                ((List<string>)hashtable[dr[0].ToString()]).Add(dr[1].ToString());
            }

            NLP.Classify.word_pooling = 0.7d;
            NLP.Classify.AgentId = agent_id;

            foreach (DictionaryEntry ht in hashtable)
            {
                NLP.Classify.TrainIntentGroup(((List<string>)hashtable[ht.Key.ToString()]).ToArray(), new string[] { ht.Key.ToString() }, true, 10);
            }

            //Console.WriteLine("TrainDb Finished");
        }


        public static async Task ClearDataset(string agent_id)
        {
            Data.Query("DELETE FROM nlp_dataset WHERE agent_id=?agent_id;", new string[] { agent_id });
        }
    }
}
