
namespace NLP.Models
{
    public class Answer
    {
        public int answer_id { get; set; }
        public int intent_id { get; set; }
        public string name { get; set; }
        public int type { get; set; }
        public string phrase { get; set; } = "";

        #region To Next Analyze
        public int action { get; set; } //0-none,1-expect,2-redirect,3-print,4-close_session,5-close_session_instantly
        public int action_mode { get; set; }  //0-response,1-noreponse
        public string action_regex { get; set; }
        public string action_api { get; set; }
        public string action_api_authorization_header { get; set; }
        public string trigger_intent_id { get; set; }
        public string trigger_id { get; set; }
        #endregion To Next Analyze

        public int index { get; set; }
        public int parent_id { get; set; }
        public int isdefault { get; set; }


        public Answer()
        {

        }

        public static Answer Instance()
        {
            return new Answer();
        }

        public static Answer Instance(int answer_id)
        {
            return MySQL.Json.Select.Fill(MySQL.Data.Query("SELECT * FROM nlp_answers WHERE answer_id=?answer_id", new string[] { answer_id.ToString() })).Single<Answer>();
        }


        public static Answer Instance(string intent_id, string? answer_id)
        {
            if (answer_id != null)
            {
                return MySQL.Json.Select.Fill(MySQL.Data.Query("SELECT * FROM nlp_answers WHERE intent_id=?intent_id AND answer_id=?answer_id", new string[] { intent_id, answer_id })).Single<Answer>();
            }
            else
            {
                return MySQL.Json.Select.Fill(MySQL.Data.Query("SELECT * FROM nlp_answers WHERE intent_id=?intent_id ORDER BY index ASC LIMIT 1", new string[] { intent_id })).Single<Answer>();
            }
        }
    }
}
