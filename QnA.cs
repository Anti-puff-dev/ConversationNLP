
using MySQL;

namespace NLP
{
    public class QnA
    {

        public static string IntentId = "";
        public static string DbQTable = "nlp_questions";
        public static string DbATable = "nlp_answers";
        public static string DbConnection
        {
            get => MySQL.DbConnection.ConnString;
            set { MySQL.DbConnection.ConnString = value; }
        }
        public static float PoolingRate = 0.7f;
        public static float SimilarityThreshold = 0.4f;

        public static void Train(string question, string answer, string? Intent_id = null)
        {
            if (Intent_id != null) IntentId = Intent_id;

            string answer_id = "";
            try
            {
                answer_id = Data.Query($"SELECT answer_id FROM {DbATable} WHERE phrase=?phrase AND intent_id=?intent_id", new string[] { answer, IntentId }).Tables[0].Rows[0][0].ToString();
            }
            catch (Exception err) { }


            if (String.IsNullOrEmpty(answer_id))
            {
                answer_id = Data.Query($"INSERT INTO {DbATable} (intent_id, phrase) VALUES (?intent_id, ?phrase);SELECT LAST_INSERT_ID();", new string[] { IntentId, answer }).Tables[0].Rows[0][0].ToString();
            }

            Data.Query($"INSERT IGNORE {DbQTable} (intent_id, answer_id, phrase) VALUES (?intent_id, ?answer_id, ?phrase)", new string[] { IntentId, answer_id, question });
        }


        public static void Train(string[] questions, string answer, string? Intent_id = null)
        {
            if (Intent_id != null) IntentId = Intent_id;

            foreach (string question in questions)
            {
                Train(question, answer);
            }
        }


        public static Models.QnA.Result Predict(string text, string? Intent_id = null)
        {
            if (Intent_id != null) IntentId = Intent_id;

            text = Sanitize.HardApply(text);
            string[] list = text.Split(new char[] { ' ', '\t' });
            string[] words = list.Distinct().ToArray<string>();

            string _match = "";

            int c = 0;
            foreach (string token in words)
            {
                string ptoken = Tokenize.WordPooling(token, PoolingRate);
                if (token.Length > 2) _match += (_match == "" ? "" : ",") + ("\"" + token + "\"");
                if (ptoken.Length > 2) _match += (_match == "" ? "" : ",") + ("" + ptoken + "*");
                c++;
            }

            string query = $"SET @q:='{text}';SET @m:='{_match}';SELECT question_id, {DbQTable}.answer_id,{DbATable}.phrase, (MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE)) AS relevance,(SELECT SIMILARITY_STRING(@q, {DbQTable}.phrase)) AS distance FROM {DbQTable} INNER JOIN {DbATable} ON {DbQTable}.answer_id={DbATable}.answer_id WHERE {DbQTable}.intent_id=?intent_id AND MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE)>0 AND (SELECT SIMILARITY_STRING(@q, {DbQTable}.phrase))>{(SimilarityThreshold * 10)} ORDER BY (distance*(MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE))) DESC LIMIT 1;";

            Models.QnA.Result _result = MySQL.Json.Select.Fill(Data.Query(query, new string[] { IntentId })).Single<Models.QnA.Result>();

            return _result;
        }


        public static Models.QnA.Result[] Predict(string text, int results, string? intent_id = null)
        {
            if (intent_id != null) IntentId = intent_id;

            text = Sanitize.HardApply(text);
            string[] list = text.Split(new char[] { ' ', '\t' });
            string[] words = list.Distinct().ToArray<string>();

            string _match = "";

            int c = 0;
            foreach (string token in words)
            {
                string ptoken = Tokenize.WordPooling(token, PoolingRate);
                if (token.Length > 2) _match += (_match == "" ? "" : ",") + ("\"" + token + "\"");
                if (ptoken.Length > 2) _match += (_match == "" ? "" : ",") + ("" + ptoken + "*");
                c++;
            }

            //Console.WriteLine($"SET @q:='{text}';SET @m:='{_match}';SELECT question_id, {DbQTable}.answer_id,{DbATable}.phrase, (MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE)) AS relevance,(SELECT SIMILARITY_STRING(@q, {DbQTable}.phrase)) AS distance FROM {DbQTable} INNER JOIN {DbATable} ON {DbQTable}.answer_id={DbATable}.answer_id WHERE {DbQTable}.intent_id=?intent_id AND MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE)>0 AND (SELECT SIMILARITY_STRING(@q, {DbQTable}.phrase))>{(SimilarityThreshold * 10)} ORDER BY (distance*(MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE))) DESC LIMIT {results};");
            string query = $"SET @q:='{text}';SET @m:='{_match}';SELECT question_id, {DbQTable}.answer_id,{DbATable}.phrase, (MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE)) AS relevance,(SELECT SIMILARITY_STRING(@q, {DbQTable}.phrase)) AS distance FROM {DbQTable} INNER JOIN {DbATable} ON {DbQTable}.answer_id={DbATable}.answer_id WHERE {DbQTable}.intent_id=?intent_id AND MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE)>0 AND (SELECT SIMILARITY_STRING(@q, {DbQTable}.phrase))>{(SimilarityThreshold * 10)} ORDER BY (distance*(MATCH({DbQTable}.phrase) AGAINST(@m IN BOOLEAN MODE))) DESC LIMIT {results};";
            Models.QnA.Result[] _results = MySQL.Json.Select.Fill(Data.Query(query, new string[] { IntentId })).Multiple<Models.QnA.Result>();
            return _results;
        }

    }
}
