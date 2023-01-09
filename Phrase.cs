using System.Collections;
using System.Data;


namespace NLP
{
    public class Phrase
    {
        public static string[]? Similar(string text, int results = 10)
        {
            try
            {
                List<string> result = new List<string>();
                string[] list = text.Split(new char[] { ' ', '\t' });

                Hashtable words = new Hashtable();

                foreach (string word in list)
                {
                    if (words.ContainsKey(word))
                    {
                        ((List<string>)words[word]).Add(word);
                    }
                    else
                    {
                        words[word] = new List<string>();
                        ((List<string>)words[word]).Add(word);
                    }


     
                }


                foreach (string word in list)
                {
                    DataSet ds = MySQL.Data.Query(QSinonimos, new string[] { word });

                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {

                            if (words.ContainsKey(word))
                            {
                                ((List<string>)words[word]).Add(dr["sword"].ToString());
                            }
                            else
                            {
                                words[word] = new List<string>();
                                ((List<string>)words[word]).Add(dr["sword"].ToString());
                            }
                        }
                    }
                    else
                    {
                        if (words.ContainsKey(word))
                        {
                            ((List<string>)words[word]).Add(word);
                        }
                        else
                        {
                            words[word] = new List<string>();
                            ((List<string>)words[word]).Add(word);
                        }
                    }

                }


                for (int i = 0; i < results * 3; i++)
                {
                    List<string> _phrase = new List<string>();

                    foreach (string word in list)
                    {
                        string _word = GetRandom((List<string>)words[word]);
                        _phrase.Add(_word);
                    }

                    string r = String.Join(" ", _phrase);
                    if (r != text && !result.Contains(r)) result.Add(r);
                }

                return result.Take(results).ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.InnerException.Message);
            }
            return null;
        }


        private static string GetRandom(List<string> list)
        {
            var random = new Random();
            int index = random.Next(list.Count);
            return list[index];
        }


        private static bool Check(string word)
        {
            string[] words = new string[] { "de", "das", "da", "do", "na", "nas", "no", "nos", "em", "o", "a", "as", "os", "aos", "é", "e", "com", "que" };

            return words.Contains(word);
        }



        #region Queries
        public static string QSinonimos = @"
SELECT 
	maria_words.id,
	maria_words.word,
	maria_words.grammar,
	maria_synonyms.sword_id,
	mw.word AS sword,
	mw.grammar AS sgrammar

FROM maria_words

INNER JOIN maria_synonyms ON maria_synonyms.word_id=maria_words.id
INNER JOIN maria_words AS mw ON mw.id=maria_synonyms.sword_id 

WHERE 
	maria_words.word=?word 
	AND mw.grammar=maria_words.grammar LIMIT 10";

        #endregion Queries


    }
}
