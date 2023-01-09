using MySQL;

namespace NLP
{
    public static class Extensions
    {
        public static int IndexOf<T>(this IEnumerable<T> list, Predicate<T> condition)
        {
            int i = -1;
            return list.Any(x => { i++; return condition(x); }) ? i : -1;
        }

        public static IEnumerable<TSource> DistinctBy<TSource>(this IEnumerable<TSource> source, params Func<TSource, object>[] keySelectors)
        {
            var seenKeysTable = keySelectors.ToDictionary(x => x, x => new HashSet<object>());

            foreach (var element in source)
            {
                var flag = true;
                foreach (var (keySelector, hashSet) in seenKeysTable)
                {
                    flag = flag && hashSet.Add(keySelector(element));
                }

                if (flag)
                {
                    yield return element;
                }
            }
        }
    }


    public class Classify
    {
        public static string AgentId = "";
        public static string DbAgents = "nlp_agents";
        public static string DbIntents = "nlp_intents";
        public static string DbTable = "nlp_dataset";
        public static double TrainingRate = 1.1;
        public static double TrainingRateDecay = 1.1;

        public static string DbConnection
        {
            get => MySQL.DbConnection.ConnString;
            set { MySQL.DbConnection.ConnString = value; }
        }

        public static double word_pooling = 0.7d;
        public static int maxlength = 0;
        public static bool soundex = false;

        public Classify()
        {

        }

        public static Classify Instance()
        {
            return new Classify();
        }

        public static Classify Instance(double word_pooling, int maxlength, bool sondex = false)
        {
            Classify.word_pooling = word_pooling;
            Classify.maxlength = maxlength;
            Classify.soundex = sondex;
            return new Classify();
        }



        #region Train
        #region Train.Intent
        public static void TrainIntent(string text, string[] words)
        {
            Models.Token[] word_tokens = null;
            Models.Token[] tokensArr = Tokenize.Instance(word_pooling, maxlength, soundex).Apply(text);
            Models.Token[] tokens = Relevances(Weights(tokensArr));

            int level = 0;

            foreach (string word in words)
            {
                int intent_id = Convert.ToInt32(word);

                foreach (Models.Token _token in tokens)
                {
                    _token.intent_id = intent_id;
                    word_tokens = MySQL.Json.Select.Fill($"SELECT * FROM {DbTable} WHERE word=?word AND agent_id=?agent_id AND level=?level ORDER BY word ASC", new string[] { _token.word, AgentId, level.ToString() }).Multiple<Models.Token>();

                    if (word_tokens.Count() > 0)
                    {
                        //int maxCount = word_tokens.OrderByDescending(i => i.count).First().count;

                        foreach (Models.Token wtoken in word_tokens)
                        {
                            string query = "";
                            List<string> parms = new List<string>();
                            int c = 0;

                            Models.Token token = MySQL.Json.Select.Fill(Data.Query($"SELECT * FROM {DbTable} WHERE word=?word AND intent_id=?intent_id AND agent_id=?agent_id AND level=?level ORDER BY word ASC LIMIT 1", new string[] { _token.word, intent_id.ToString(), AgentId, level.ToString() })).Single<Models.Token>();
                            if (token.id > 0)
                            {
                                Data.Query($"UPDATE {DbTable} SET {DbTable}.count={DbTable}.count+1 WHERE id=?id", new string[] { token.id.ToString() });
                            }
                            else
                            {
                                token = _token;
                            }

                            string query_insert = $"INSERT IGNORE INTO {DbTable} (agent_id, word, intent_id, level, {DbTable}.count, weight, relevance) VALUES (?agent_i{c}, ?word_i{c}, ?intent_i{c}, ?level{c}, ?count_i{c}, ?weight_i{c}, ?relevance_i{c})";
                            List<string> parms_insert = new List<string>();
                            parms_insert.Add(AgentId);
                            parms_insert.Add(_token.word);
                            parms_insert.Add(intent_id.ToString());
                            parms_insert.Add(level.ToString());
                            parms_insert.Add(_token.count.ToString());
                            parms_insert.Add(_token.weight.ToString().Replace(",", "."));
                            parms_insert.Add(_token.relevance.ToString().Replace(",", "."));
                            Data.Query(query_insert, parms_insert.ToArray());




                            if (wtoken.intent_id == intent_id)
                            {
                                double weight = token.weight * (TrainingRate) * (1 + (token.count / 1000));
                                double relevance = token.relevance * (TrainingRate) * (1 + (token.count / 1000));

                                query += $"UPDATE {DbTable} SET weight=?weight{c}, relevance=?relevance{c} WHERE word=?word{c} AND level={level};";
                                parms.Add(weight.ToString().Replace(",", "."));
                                parms.Add(relevance.ToString().Replace(",", "."));
                                parms.Add(wtoken.word);
                            }
                            else
                            {
                                double weight = token.weight / (TrainingRateDecay);
                                double relevance = token.relevance / (TrainingRateDecay);

                                query += $"UPDATE {DbTable} SET weight=?weight{c}, relevance=?relevance{c} WHERE word=?word{c} AND level={level};";
                                parms.Add(weight.ToString().Replace(",", "."));
                                parms.Add(relevance.ToString().Replace(",", "."));
                                parms.Add(wtoken.word);
                            }
                            c++;
                            if (query != "") Data.Query(query, parms.ToArray());

                        }
                    }
                    else
                    {
                        string query = "";
                        List<string> parms = new List<string>();
                        int c = 0;

                        query += $"INSERT IGNORE INTO {DbTable} (agent_id, word, intent_id, level, {DbTable}.count, weight, relevance) VALUES (?agent_i{c}, ?word_i{c}, ?intent_i{c}, ?level{c}, ?count_i{c}, ?weight_i{c}, ?relevance_i{c}) ON DUPLICATE KEY UPDATE {DbTable}.count=?count_u{c}, weight=?weight_u{c}, relevance=?relevance_u{c};";
                        parms.Add(AgentId);
                        parms.Add(_token.word);
                        parms.Add(intent_id.ToString());
                        parms.Add(level.ToString());
                        parms.Add(_token.count.ToString());
                        parms.Add(_token.weight.ToString().Replace(",", "."));
                        parms.Add(_token.relevance.ToString().Replace(",", "."));
                        parms.Add(_token.count.ToString());
                        parms.Add(_token.weight.ToString().Replace(",", "."));
                        parms.Add(_token.relevance.ToString().Replace(",", "."));
                        c++;
                        Data.Query(query, parms.ToArray());
                    }
                }

                level++;

                #region Debug
                /*Console.WriteLine(">>> ");
                foreach (Models.Token token in tokens.OrderByDescending(o => o.weight))
                {
                    Console.WriteLine($"word: {token.word} \t category: {token.category_id} \t count: {token.count} \t weight: {token.weight} \t relevance: {token.relevance}");
                }*/
                #endregion Debug
            }


        }


        public static void TrainIntent(string text, string[] words, string[] ignore)
        {
            text = Sanitize.CustomApply(text, ignore);
            TrainIntent(text, words);
        }


        public static void TrainIntent(string text, string[] words, bool ignore)
        {
            if (ignore)
            {
                text = Sanitize.HardApply(text);
            }
            else
            {
                text = Sanitize.Apply(text);
            }

            TrainIntent(text, words);
        }


        public static void TrainIntentGroup(string[] texts, string[] words, int epochs = 10)
        {
            Console.WriteLine("Start Training Group...");
            TrainingRate = 1 + ((TrainingRate - 1) / epochs);

            for (int j = 0; j < epochs; j++)
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    texts[i] = Sanitize.Apply(texts[i]);
                    TrainIntent(texts[i], words);
                }
            }
        }


        public static void TrainIntentGroup(string[] texts, string[] words, string[] ignore, int epochs = 1)
        {
            Console.WriteLine("Start Training Group...");
            TrainingRate = 1 + ((TrainingRate - 1) / epochs);

            for (int j = 0; j < epochs; j++)
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    texts[i] = Sanitize.CustomApply(texts[i], ignore);
                    TrainIntent(texts[i], words);
                }
            }
        }


        public static void TrainIntentGroup(string[] texts, string[] words, bool ignore, int epochs = 10)
        {
            Console.WriteLine("Start Training Group...");
            TrainingRate = 1 + ((TrainingRate - 1) / epochs);

            for (int j = 0; j < epochs; j++)
            {
                Console.WriteLine(">> Epoch " + j);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (ignore)
                    {
                        texts[i] = Sanitize.HardApply(texts[i]);
                    }
                    else
                    {
                        texts[i] = Sanitize.Apply(texts[i]);
                    }
                    TrainIntent(texts[i], words);
                }
            }
        }

        #endregion Train.Intent
        #endregion Train


        #region Predict
        public static Models.Intent[] Predict(string text, int subcategories_levels = 1, int results = 10)
        {
            Models.Token[] tokens = Relevances(Weights(Tokenize.Instance(word_pooling, maxlength, soundex).Apply(text)));
            List<Models.Token[]> list = new List<Models.Token[]>();
            List<Models.Intent> list_categories = new List<Models.Intent>();


            Models.Token[] _tokens = tokens.OrderByDescending(i => i.weight).ToArray();


            if (subcategories_levels > 0)
            {
                subcategories_levels--;

                foreach (Models.Token token in _tokens)
                {
                    //Console.WriteLine($"SELECT nlp_dataset.*  FROM {DbTable} INNER JOIN {DbIntents} ON {DbIntents}.intent_id={DbTable}.intent_id AND {DbIntents}.agent_id={DbTable}.agent_id WHERE {DbTable}.agent_id={AgentId} AND word='{token.word}' AND {DbTable}.level=0 ORDER BY weight DESC LIMIT 30");
                    Models.Token[] word_tokens = MySQL.Json.Select.Fill($"SELECT nlp_dataset.*  FROM {DbTable} INNER JOIN {DbIntents} ON {DbIntents}.intent_id={DbTable}.intent_id AND {DbIntents}.agent_id={DbTable}.agent_id WHERE {DbTable}.agent_id=?agent_id AND word=?word AND {DbTable}.level=0 ORDER BY weight DESC LIMIT 30", new string[] { AgentId, token.word }).Multiple<Models.Token>();
                    if (word_tokens != null && word_tokens.Length > 0) list.Add(word_tokens);
                }

                int c = 0;
                foreach (Models.Token[] token_list in list)
                {
                    foreach (Models.Token token in token_list)
                    {
                        Models.Intent? cat = list_categories.Find(v => v.intent_id == token.intent_id);

                        if (cat != null)
                        {
                            cat.weigths_avg = (cat.weigths_avg + token.weight) / 2;
                            cat.weigths_sum += token.weight;
                            cat.relevance_avg = (cat.relevance_avg + token.relevance) / 2;
                            cat.relevance_sum += token.relevance;
                            cat.count++;
                        }
                        else
                        {
                            //Console.WriteLine($"SELECT name FROM {DbIntents} WHERE intent_id={token.intent_id}");
                            string? intent_name = Data.Query($"SELECT name FROM {DbIntents} WHERE intent_id=?intent_id", new string[] { token.intent_id.ToString() }).Tables[0].Rows[0][0].ToString();
                            list_categories.Add(new Models.Intent() { intent_id = token.intent_id, name = intent_name, count = 1, weigths_sum = token.weight, weigths_avg = token.weight, relevance_sum = token.relevance, relevance_avg = token.relevance, subcategories = PredictSubCategory(_tokens, token.intent_id, subcategories_levels, results) });
                        }
                    }
                }

                list_categories = list_categories.OrderByDescending(item => (item.weigths_avg * item.relevance_avg)).Take(results).ToList();
            }


            return NLP.Result.Normalize(list_categories.ToArray());
        }


        private static Models.Intent[] PredictSubCategory(Models.Token[] tokens, int parent_id, int subcategories_levels, int results)
        {
            List<Models.Token[]> list = new List<Models.Token[]>();
            List<Models.Intent> list_categories = new List<Models.Intent>();

            int level = 1;

            if (subcategories_levels > 0)
            {
                subcategories_levels--;

                foreach (Models.Token token in tokens)
                {
                    Models.Token[] word_tokens = MySQL.Json.Select.Fill($"SELECT nlp_dataset.*  FROM {DbTable} INNER JOIN {DbIntents} ON {DbIntents}.intent_id={DbTable}.intent_id AND {DbIntents}.agent_id={DbTable}.agent_id WHERE {DbTable}.agent_id=?agent_id AND word=?word AND {DbTable}.level=?level ORDER BY weight DESC LIMIT 30", new string[] { AgentId, token.word, level.ToString() }).Multiple<Models.Token>();
                    if (word_tokens != null && word_tokens.Length > 0) list.Add(word_tokens);
                }

                int c = 1;
                foreach (Models.Token[] token_list in list)
                {
                    foreach (Models.Token token in token_list)
                    {
                        Models.Intent? cat = list_categories.Find(v => v.intent_id == token.intent_id);
                        if (cat != null)
                        {
                            cat.weigths_avg = (cat.weigths_avg + token.weight) / 2;
                            cat.weigths_sum += token.weight;
                            cat.relevance_avg = (cat.relevance_avg + token.relevance) / 2;
                            cat.relevance_sum += token.relevance;
                            cat.count++;
                        }
                        else
                        {
                            string? intent_name = Data.Query($"SELECT name FROM {DbIntents} WHERE intent_id=?intent_id", new string[] { token.intent_id.ToString(), level.ToString() }).Tables[0].Rows[0][0].ToString();
                            list_categories.Add(new Models.Intent() { intent_id = token.intent_id, name = intent_name, count = 1, weigths_sum = token.weight, weigths_avg = token.weight, relevance_sum = token.relevance, relevance_avg = token.relevance, subcategories = PredictSubCategory(tokens, token.intent_id, subcategories_levels, results) });
                        }
                    }
                }

                list_categories = list_categories.OrderByDescending(item => (item.weigths_avg * item.relevance_avg)).Take(results).ToList();
                level++;
            }

            return NLP.Result.Normalize(list_categories.ToArray());
        }


        public static Models.Intent[] Predict(string text, bool ignore = true, int subcategories_levels = 1, int results = 10)
        {
            if (ignore)
            {
                text = Sanitize.HardApply(text);
            }
            else
            {
                text = Sanitize.Apply(text);
            }

            return Predict(text, subcategories_levels, results);
        }


        public static Models.Intent[] Predict(string text, string[] ignore, int subcategories_levels = 1, int results = 10)
        {
            text = Sanitize.CustomApply(text, ignore);
            return Predict(text, subcategories_levels, results);
        }
        #endregion Predict


        #region Functions
        public static Models.Token[] Weights(Models.Token[] tokens)
        {
            int maxCount = tokens.OrderByDescending(i => i.count).First().count;

            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i].weight = ((double)tokens[i].count / (1 + (double)maxCount));
            }

            return tokens;
        }


        public static Models.Token[] Relevances(Models.Token[] tokens, int maxDistance = 10)
        {
            Models.Token[] _tokens = tokens.OrderByDescending(i => i.weight).ToArray();
            int[] position = new int[5];


            for (int i = 0; i < _tokens.Length; i++)
            {
                double sum = 0d;
                for (int j = 0; j < position.Length; j++)
                {
                    try
                    {
                        position[j] = tokens.IndexOf(item => item.word.Equals(_tokens[i + j].word));
                    }
                    catch (Exception ex)
                    {
                        position[j] = 0;
                    }


                    if (j > 0 && position[j] > -1)
                    {
                        int distance = Math.Abs(position[j] - position[0]);

                        sum += 1d / (double)(Math.Pow(j * 2, 2) + Math.Pow(distance, 2));
                    }
                }

                _tokens[i].relevance = (double)_tokens[i].weight * sum;
            }

            return _tokens;
        }


        public static Models.Token[] Intersect(Models.Token[] arr1, Models.Token[] arr2)
        {
            List<Models.Token> result = new List<Models.Token>();
            foreach (Models.Token token1 in arr1)
            {
                foreach (Models.Token token2 in arr2)
                {
                    if (token1.word == token2.word)
                    {
                        token1.count += token2.count;
                        result.Add(token1);
                    }
                }
            }

            return result.ToArray();
        }


        public static Models.Token[] Diference(Models.Token[] arr1, Models.Token[] arr2)
        {
            List<Models.Token> result = new List<Models.Token>();


            foreach (Models.Token token1 in arr1)
            {
                bool has = false;
                Models.Token tmp = null;
                foreach (Models.Token token2 in arr2)
                {
                    if (token1.word == token2.word)
                    {
                        has = true;
                        tmp = null;
                        break;
                    }
                    else
                    {
                        tmp = token2;
                    }
                }

                if (!has)
                {
                    result.Add(token1);
                    if (tmp != null) result.Add(tmp);
                }
            }

            foreach (Models.Token token2 in arr2)
            {
                bool has = false;
                Models.Token tmp = null;

                foreach (Models.Token token1 in arr1)
                {
                    if (token1.word == token2.word)
                    {
                        has = true;
                        tmp = null;
                        break;
                    }
                    else
                    {
                        tmp = token1;
                    }
                }

                if (!has)
                {
                    result.Add(token2);
                    if (tmp != null) result.Add(tmp);
                }
            }




            return result.DistinctBy(t => t.word).ToArray();
        }


        public static void ClearDb()
        {
            Data.Query($"TRUNCATE TABLE {DbTable};");
        }
        #endregion Functions
    }
}