
using MySQL;
using Newtonsoft.Json.Linq;
using System.Reflection.Metadata.Ecma335;

namespace NLP.Models
{
    public class Session
    {
        private Answer? _expect;


        public string? uuid { get; set; }
        public string? agent_id { get; set; }
        public string? origin_id { get; set; }
        public string? origin { get; set; }
        public List<Message>? messages { get; set; } = new List<Message>();
        public Answer? expect
        {
            get { return _expect; }
            set
            {
                _expect = value;
                if (!String.IsNullOrEmpty(uuid))
                {
                    if (value == null)
                    {
                        //Trace.Message($"NO EXPECTED {uuid}");
                        Data.Query("UPDATE nlp_sessions SET expected=0 WHERE uuid=?uuid", new string[] { uuid });
                    }
                    else
                    {
                        //Trace.Message($"EXPECTED {value.answer_id.ToString()} / {uuid}");
                        Data.Query("UPDATE nlp_sessions SET expected=?answer_id WHERE uuid=?uuid", new string[] { value.answer_id.ToString(), uuid });
                    }
                }
            }
        }
        public List<FormData>? data { get; set; } = new List<FormData>();
        public DateTime date_creation { get; set; }
        public DateTime date_update { get; set; }



        public Session()
        {

        }


        public void Add(Message obj)
        {
            //Trace.Message($"{uuid} / {origin} / {obj.phrase}");
            messages?.Add(obj);
            MySQL.Json.Insert.Into(JObject.FromObject(new { uuid, obj.origin_id, obj.origin, obj.phrase, date_creation = obj.date_creation.ToString("yyyy-MM-dd HH:mm:ss") }), "nlp_sessions_messages").Run();
        }


        public void AddFormData(FormData fdata)
        {
            data?.Add(fdata);
            MySQL.Json.Insert.Into(JObject.FromObject(new { uuid, fdata.field_name, fdata.field_value, date_creation = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }), "nlp_sessions_data").Run();
        }


        public FormData[] GetFormData(string uuid) => MySQL.Json.Select.Fill(Data.Query("SELECT field_name, field_value FROM nlp_sessions_data WHERE uuid=?uuid", new string[] { uuid })).Multiple<FormData>();
       
    }


    
}
