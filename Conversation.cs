using RestSharp;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using NLP.Models;


namespace NLP
{
    public class Conversation
    {
        //public static X509Certificate certificate = X509Certificate2.CreateFromPemFile("certificate.pem", "certificate.key");




        public static string[]? Analize(Message msg, string agent_id, List<string> previous = null)
        {
            Console.WriteLine($"<<< Analize >>> ({msg.phrase})");
            int? intent_id = null;
            Answer? answer = null;

            List<string> list = new List<string>();
            if (previous != null) list.AddRange(previous);
            

            #region Session Process ----------------------------------------------------------------------------------------------------------
            Session session = Sessions.Open(agent_id, msg.origin_id, msg.origin);



            #region Grettings
            if(session.messages.Count == 0)
            {
                string gretting_start = Greetings.Start(agent_id);
                if (!String.IsNullOrEmpty(gretting_start)) list.Add(gretting_start);
            }
            #endregion Grettings


            if(session.expect == null || (session.expect != null && session?.expect?.action != 2))
            {
                session.Add(new NLP.Models.Message() { origin = msg.origin, phrase = msg.phrase, date_creation = DateTime.Now }); //From user input
            }
            #endregion Session Process ----------------------------------------------------------------------------------------------------------



            #region Expect Process ----------------------------------------------------------------------------------------------------------
            Trace.Message($"Expect Process: {session.expect?.answer_id.ToString()}");

            if (String.IsNullOrEmpty(session.expect?.answer_id.ToString()))
            {
                #region Default
                /*(string[] default_options, Answer _answer) = Options.Default(agent_id);

                if (default_options.Length > 0)
                {
                    session.expect = _answer;

                    string default_result = "";
                    if (String.IsNullOrEmpty(default_result)) default_result = "Escolha uma das opções abaixo:\n";
                    foreach (string option in default_options)
                    {
                        default_result += option + "\n";
                    }
                    session.Add(new NLP.Models.Message() { origin = "bot_" + agent_id + "_" + _answer.intent_id, phrase = default_result, date_creation = DateTime.Now }); //From bot
                    list.Add(default_result);
                    return list.ToArray();
                }*/
                #endregion Default


                #region Classify Process 
                Trace.Message($"Classify Process - IntentId: {intent_id}");
                if (intent_id == null)
                {
                    NLP.Classify.AgentId = agent_id;
                    NLP.Classify.word_pooling = 0.7d;
                    Intent[] intents = NLP.Classify.Predict(msg.phrase, true, 1, 10);

                    if (intents.Length > 0)
                    {
                        intent_id = intents[0].intent_id;

                        Trace.Message($"Phrase: {msg.phrase}");

                        NLP.Result.Print(intents);
                    }
                }

                Trace.Message($"IntentId: {intent_id}");
                if (intent_id != null)
                {
                    Models.QnA.Result[] results = NLP.QnA.Predict(msg.phrase, 10, intent_id.ToString());
                    Trace.Message($"QnA results.Length {results.Length}");


                    if (results.Length > 0)
                    {
                        int answer_id = results[0].answer_id;
                        answer = Answer.Instance(answer_id);

                        Trace.Message($"{answer.answer_id} / {answer.phrase} / Type: {answer.type} / {answer.action_api}");

                        if (answer.type == 0)
                        {
                            string[] options = Options.GetOptions(answer.intent_id.ToString(), answer.answer_id.ToString());

                            Trace.Message($"Options Length: {options.Length} / Intent Id: {answer.intent_id} / Answer Id: {answer.answer_id}");

                            if (options.Length > 0)
                            {
                                session.expect = answer;
                                Trace.Message($"session.expect: {answer.answer_id}");

                                string result = "";
                                if (String.IsNullOrEmpty(result)) result = "Escolha uma das opções abaixo:\n";
                                foreach (string option in options)
                                {
                                    result += option + "\n";
                                }
                                session.Add(new NLP.Models.Message() { origin = "bot_" + agent_id + "_" + intent_id, phrase = result, date_creation = DateTime.Now }); //From bot

                                return new string[] { result };
                            }
                        }

                        Trace.Message($"Answer Type: {answer.type}");

                        if (answer.action == 1 || answer.action == 2 || answer.type == 0)
                        {
                            session.expect = answer;
                        }
                        else
                        {
                            session.expect = null;
                        }


                        Trace.Message($"Will Trigger {answer.trigger_intent_id} {answer.trigger_id}");
                        if (!String.IsNullOrEmpty(answer.phrase))
                        {
                            session.Add(new NLP.Models.Message() { origin = "bot_" + agent_id + "_" + intent_id, phrase = answer.phrase, date_creation = DateTime.Now }); //From bot
                            list.Add(answer.phrase);
                        }

                        if (answer.action == 2) //redirect
                        {
                            return Analize(msg, agent_id, list);
                        }
                        else if (answer.action == 4) // Close Session next
                        {
                            Sessions.Close(session.uuid);
                            string gretting_end = Greetings.End(agent_id);
                            if (!String.IsNullOrEmpty(gretting_end)) list.Add(gretting_end);
                        }
                        return list.ToArray();
                    }
                }
                #endregion Classify Process 
            }
            else
            {
                Trace.Message($"Expect Action {session.expect.action}");

                if(session.expect.type == 0) //Type Options
                {
                    Trace.Message($"Type {session.expect?.type}");

                    Trace.Message($"Type Options {session.expect.phrase} / {msg.phrase} / {session.expect.intent_id} / {session.expect.answer_id}");
                    answer = Options.Find(msg.phrase, session.expect.intent_id.ToString(), session.expect.answer_id.ToString());
                    Trace.Message($"Options: {(answer == null ? "IsNULL" : "Exists")}");

                    string? result = "";

                    if (answer == null)
                    {
                        answer = session.expect;
                    }

                    if (answer.index != 0)
                    {
                        session.Add(new Message() { origin_id = msg.origin_id, origin = msg.origin, phrase = $"Opção escolhida {answer.index}. {msg.phrase}", date_creation = DateTime.Now }); //From user input
                        if (answer.trigger_id != "0") session.expect = answer;
                    }


                    if (answer.trigger_id != "0")
                    {
                        Answer tmp_answer = Options.Trigger(answer.trigger_id);
                        session.expect = tmp_answer;

                        if (tmp_answer.type == 1)
                        {
                            return Analize(msg, agent_id, list);
                        }
                    }


                    if (answer.trigger_id == "0")
                    {
                        return new string[] { "A opção escolhida é invalida tente novamente", session.expect.phrase };
                    }


                    string[] options = Options.GetOptions(answer.intent_id.ToString(), answer.trigger_id);

                    Trace.Message($"options.Length {options.Length}");
                    if (options.Length > 0)
                    {
                        result = "";
                        if (String.IsNullOrEmpty(result)) result = "Escolha uma das opções abaixo:\n";
                        foreach (string option in options)
                        {
                            result += option + "\n";
                        }
                        session.Add(new NLP.Models.Message() { origin = "bot_" + agent_id + "_" + intent_id, phrase = result, date_creation = DateTime.Now }); //From bot
                        if (session.expect?.action == 4)
                        {
                            Sessions.Close(session);
                        }
                        return new string[] { result };

                    }
                    else
                    {
                        if (answer == null) return new string[] { "A opção escolhida é invalida tente novamente", session.expect.phrase };
                    }

                    
                }
                else if (session.expect.type == 1)
                {
                    bool validation = true;

                    #region Regex Validation
                    if (!String.IsNullOrEmpty(session.expect.action_regex)) //regex validation
                    {
                        string[] expressions = session.expect.action_regex.Split(new char[] { '|' });

                        foreach (string expression in expressions)
                        {
                            string exp = expression;
                            if (expression == "email")
                            {
                                exp = @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$";
                            }
                            else if (expression == "telefone")
                            {
                                exp = @"^\(?(?:[14689][1-9]|2[12478]|3[1234578]|5[1345]|7[134579])\)? ?(?:[2-8]|9[1-9])[0-9]{3}\-?[0-9]{4}$";
                            }

                            validation = Regex.IsMatch(msg.phrase, @exp);
                            if (!validation)
                            {
                                return new string[] { "A informação digitada é invalida tente novamente", session.expect.phrase };
                            }
                        }
                    }
                    #endregion Regex Validation
                }



                #region Action / Mode
                if (session.expect.action == 4) //close session
                {
                    string gretting_end = Greetings.End(agent_id);
                    if (!String.IsNullOrEmpty(session.expect.phrase)) list.Add(session.expect.phrase);
                    if (!String.IsNullOrEmpty(gretting_end)) list.Add(gretting_end);
                    Sessions.Close(session.uuid);
                    session.expect = null;
                    return list.ToArray();
                }
                else if (session.expect.action == 3) //print
                {
                    return null;
                }
                else if (session.expect.action == 1) //expect
                {
                    intent_id = session.expect.intent_id;

                    if (!String.IsNullOrEmpty(session.expect.name)) //Add to formdata
                    {
                        //session.data?.Add(new NLP.Models.FormData() { field_name = session.expect.name, field_value = msg.phrase });
                        session.AddFormData(new NLP.Models.FormData() { field_name = session.expect.name, field_value = msg.phrase });
                    }
                }

                if (session.expect.action_mode == 1)
                {
                    session.expect = null;
                    return null;
                }
                #endregion  Action / Mode
            }
            #endregion Expect Process ----------------------------------------------------------------------------------------------------------



            #region Api ----------------------------------------------------------------------------------------------------------------
            //Console.WriteLine("Api");
            if (!String.IsNullOrEmpty(session.expect?.action_api)) //api
            {
                var options = new RestClientOptions(session.expect?.action_api)
                {
                    MaxTimeout = -1,
                    FollowRedirects = false,
                    //ClientCertificates = new X509CertificateCollection() { certificate }
                };

                var client = new RestClient(options);

                var request = new RestRequest();
                request.Method = Method.Post;
                if (!String.IsNullOrEmpty(session.expect?.action_api_authorization_header)) request.AddHeader("Authorization", $"{session.expect?.action_api_authorization_header}");
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                foreach (NLP.Models.FormData data in session.GetFormData(session.uuid))
                {
                    Trace.Message($"{data.field_name}={data.field_value}");
                    request.AddParameter(data.field_name, data.field_value);
                }

                var response = client.Execute(request);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Sessions.Close(session);
                    return new string[] { "Ocorreu um erro no processamento de sua mensagem, tente novamente mais tarde" };
                }

            }
            #endregion Api ----------------------------------------------------------------------------------------------------------------


            #region Trigger ----------------------------------------------------------------------------------------------------------------
            Trace.Message($"Trigger: {session.expect?.trigger_intent_id} / {session.expect?.trigger_id}");
            if (!String.IsNullOrEmpty(session.expect?.trigger_intent_id))
            {
                if (!String.IsNullOrEmpty(session.expect?.trigger_id))
                {
                    answer = Answer.Instance(session.expect.trigger_intent_id, session.expect?.trigger_id);
                }
                else
                {
                    answer = Answer.Instance(session.expect.trigger_intent_id, null);
                }

                if (answer.action == 1 || answer.action == 2)
                {
                    session.expect = answer;
                }
                else
                {
                    session.expect = null;
                }


                if (!String.IsNullOrEmpty(answer.phrase)) 
                {
                    session.Add(new NLP.Models.Message() { origin = "bot_" + agent_id + "_" + intent_id, phrase = answer.phrase, date_creation = DateTime.Now }); //From bot
                    list.Add(answer.phrase);
                }

                if (answer.action == 2)
                {
                    return Analize(msg, agent_id, list);
                }
                else
                {
                    return list.ToArray();
                }
            }
            #endregion Trigger ----------------------------------------------------------------------------------------------------------------




            if (list.Count > 0)
            {
                return list.ToArray();
            }
            else
            {
                return Result(session, agent_id);
            }
        }



        private static string[]? Result(Session session, string agent_id)
        {
            string? gretting_noresult = Greetings.NoResult(agent_id);

            if (session.expect != null)
            {
                if (!String.IsNullOrEmpty(gretting_noresult))
                {
                    return new string[] { gretting_noresult, session.expect.phrase };
                }
                else
                {
                    return new string[] { session.expect.phrase };
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(gretting_noresult))
                {
                    return new string[] { gretting_noresult };
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
