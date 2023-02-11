using Newtonsoft.Json;

namespace CosmosBulkUpdateSample
{
    public class User
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Area { get; set; }
        public string Country { get; set; }
        public int Followers { get; set; }
        public string Bio { get; set; }
        public MemberShipGrade Grade { get; set; }
        public string Email { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public enum MemberShipGrade
    {
        Premium,
        Standard,
        Basic
    }

}
