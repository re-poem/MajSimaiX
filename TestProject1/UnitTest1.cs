
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace TestProject1
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper output;

        public UnitTest1(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Test1()
        {
            var maidata = new Maidata("D:\\_Game\\maimaiÆ×\\TheIdolmaster\\maidata.txt");
            
            output.WriteLine(maidata.title);
            output.WriteLine(maidata.artist);

            foreach(var fumen in maidata.fumens) { 
                if (fumen == null) continue;
                var simaiInner = new SimaiProcess(fumen);
                output.WriteLine(simaiInner.notelist.Count.ToString());
                output.WriteLine(JsonConvert.SerializeObject(simaiInner.notelist));
            }
        }
    }
}