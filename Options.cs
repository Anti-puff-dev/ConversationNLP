using NLP.Models;
using System.Data;

namespace NLP
{
    public class Options
    {
        public static string[] Get(string intent_id, string trigger_id)
        {
            try
            {
                List<string> list = new List<string>(); ;
                DataSet ds = MySQL.Data.Query(QGet, new string[] { intent_id, trigger_id });
                //Console.WriteLine($"Options Count: {ds.Tables[0].Rows.Count} - {intent_id} / {answer_id}");
                if (ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        list.Add(row[0].ToString());
                    }
                    return list.ToArray();
                }
            }
            catch (Exception ex)
            {

            }

            //return new string[] { "Não há opções disponíveis no momento" };
            return new string[] { };
        }


        public static string[] GetOptions(string intent_id, string answer_id)
        {
            try
            {
                List<string> list = new List<string>();
                DataSet ds = MySQL.Data.Query(QOptions, new string[] { intent_id, answer_id, answer_id });

                if (ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        list.Add(row[0].ToString());
                    }
                    return list.ToArray();
                }
            }
            catch (Exception ex)
            {

            }
            return new string[] { };
        }


        public static Answer? Find(string msg, string intent_id, string answer_id)
        {
            Answer answer = null;
            //Console.WriteLine($"Find {msg} {intent_id} {answer_id}");

            try
            {
                answer = MySQL.Json.Select.Fill(MySQL.Data.Query(QFind, new string[] { intent_id.ToString(), answer_id.ToString(), answer_id.ToString(), msg })).Single<Answer>();

                if (answer.answer_id == 0) answer = null;
            }
            catch (Exception ex)
            {
                answer = null;
            }

            return answer;
        }


        public static Answer? Trigger(string trigger_id)
        {
            Answer answer = null;

            try
            {
                answer = MySQL.Json.Select.Fill(MySQL.Data.Query(QTrigger, new string[] { trigger_id })).Single<Answer>();

                if (answer.answer_id == 0) answer = null;
            }
            catch (Exception ex)
            {
                answer = null;
            }

            return answer;
        }


        public static (string[], Answer) Default(string agent_id)
        {
            try
            {
                List<string> list = new List<string>(); ;
                DataSet ds = MySQL.Data.Query(QDefault, new string[] { agent_id });
                if (ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        list.Add(row[0].ToString());
                    }
                    return (list.ToArray(), MySQL.Json.Select.Fill(MySQL.Data.Query(QAnswerDefault, agent_id)).Single<Answer>());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }

            return (new string[] { }, null);
        }



        #region Queries
        static string QGet = @"
SELECT CONCAT('(',na.index, '). ',na.phrase) AS phrase

FROM nlp_answers

INNER JOIN nlp_answers AS na ON na.parent_id=nlp_answers.trigger_id OR na.answer_id=nlp_answers.answer_id 


WHERE 
	nlp_answers.intent_id=?intent 
	AND nlp_answers.trigger_id=?trigger_id 
";


        static string QFind = @"
SELECT *
FROM nlp_answers
WHERE 
    intent_id=?intent_id 
	AND (answer_id=?answer_id OR parent_id=?answer_id1) 
	AND nlp_answers.index=?msg 
";


        static string QDefault = @"
SELECT CONCAT('(',na.index, '). ', na.phrase) AS phrase
FROM nlp_answers
INNER JOIN nlp_answers AS na ON na.parent_id=nlp_answers.answer_id OR na.answer_id=nlp_answers.answer_id 
WHERE nlp_answers.isdefault=1 AND nlp_answers.agent_id=?agent_id; 
";


        static string QAnswerDefault = @"
SELECT * FROM nlp_answers WHERE isdefault=1 AND agent_id=?agent_id LIMIT 1
";


        static string QTrigger = @"
SELECT * FROM nlp_answers WHERE answer_id=?answer_id 
";


        static string QOptions = @"
SELECT CONCAT('(',nlp_answers.index, '). ',nlp_answers.phrase) AS phrase
FROM nlp_answers
WHERE 
	nlp_answers.intent_id=?intent_id 
	AND (nlp_answers.parent_id=?answer_id OR nlp_answers.answer_id=?answer_id1) ORDER BY nlp_answers.index ASC 
";
        #endregion Queries
    }
}
