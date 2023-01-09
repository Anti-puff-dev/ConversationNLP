using MySQL;

namespace NLP
{
    public class Greetings
    {
        public static string? Start(string agent_id)
        {
            string? r = null;

            try
            {
                r =Data.Query("SELECT phrase FROM nlp_greetings WHERE agent_id=?agent_id AND type=?type", new string[] { agent_id, "0" }).Tables[0].Rows[0][0].ToString();
            }
            catch (Exception ex) { }

            return r == "" ? null : r;
        }

        public static string? End(string agent_id)
        {
            string? r = null;

            try
            {
                r = Data.Query("SELECT phrase FROM nlp_greetings WHERE agent_id=?agent_id AND type=?type", new string[] { agent_id, "1" }).Tables[0].Rows[0][0].ToString();
            }
            catch (Exception ex) { }

            return r == "" ? null : r;
        }

        public static string? NoResult(string agent_id)
        {
            string? r = null;

            try
            {
                r = Data.Query("SELECT phrase FROM nlp_ggreetings WHERE agent_id=?agent_id AND type=?type", new string[] { agent_id, "2" }).Tables[0].Rows[0][0].ToString();
            }
            catch (Exception ex) { }

            return r == "" ? null : r;
        }


    }
}
