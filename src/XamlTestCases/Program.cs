using CoreWf;
using CoreWf.XamlIntegration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XamlTestCases
{
    class Program
    {
        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        static void Main(string[] args)
        {
            ActivityXamlServicesSettings settings = new CoreWf.XamlIntegration.ActivityXamlServicesSettings { CompileExpressions = true };

            try
            {
                string InWriteActivity = @"
                <Activity x:Class=""WFTemplate""  
                xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""  
                xmlns:s=""clr-namespace:System;assembly=mscorlib""  
                xmlns:s1=""clr-namespace:System;assembly=System""  
                xmlns:sa=""clr-namespace:CoreWf;assembly=CoreWf""  
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">     
                </Activity>";
                var act1 = CoreWf.XamlIntegration.ActivityXamlServices.Load(GenerateStreamFromString(InWriteActivity), settings);
                WorkflowInvoker.Invoke(act1);
            }
            catch (Exception ex)
            { Console.WriteLine(ex.Message.ToString()); }
            Console.WriteLine("------------------");

            try
            {
                string XamlText = @"
            <Activity x:Class=""WFTemplate"" 
            xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities"" 
            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">   
            <Sequence>     
            <WriteLine Text=""HelloWorld"" />   </Sequence> </Activity>";
                var act10 = CoreWf.XamlIntegration.ActivityXamlServices.Load(GenerateStreamFromString(XamlText), settings);
                WorkflowInvoker.Invoke(act10);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }


            Console.WriteLine("------------------");
            try
            {
                string InWriteActivity = @"
<Activity x:Class=""WFTemplate""  
xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""  
xmlns:s=""clr-namespace:System;assembly=mscorlib""  
xmlns:s1=""clr-namespace:System;assembly=System""  
xmlns:sa=""clr-namespace:CoreWf;assembly=CoreWf""  
xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">   
<x:Members>         
<x:Property Name=""myInput"" Type=""InArgument(x:String)"" />   
</x:Members>   
<WriteLine Text=""[myInput]"" /> 
</Activity>";
                var act3 = CoreWf.XamlIntegration.ActivityXamlServices.Load(GenerateStreamFromString(InWriteActivity), settings);
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic.Add("myInput", "HelloWorld");
                WorkflowInvoker.Invoke(act3, dic);
            }
            catch (Exception ex)
            { Console.WriteLine(ex.Message.ToString()); }
            Console.WriteLine("------------------");





            try
            {

                string InWriteActivity = @"
<Activity x:Class=""WFTemplate""  
xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""  
xmlns:s=""clr-namespace:System;assembly=mscorlib""  
xmlns:s1=""clr-namespace:System;assembly=System""  
xmlns:sa=""clr-namespace:CoreWf;assembly=CoreWf""  
xmlns:hw=""clr-namespace:XamlTestCases;assembly=XamlTestCases""
xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">   
<x:Members>         
<x:Property Name=""myInput"" Type=""InArgument(x:String)"" />   
</x:Members>   
<HelloWorldConsole Text=""[myInput]"" /> 
</Activity>";
                var act2 = CoreWf.XamlIntegration.ActivityXamlServices.Load(GenerateStreamFromString(InWriteActivity), settings);
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic.Add("myInput", "HelloWorld");
                WorkflowInvoker.Invoke(act2, dic);
            }
            catch (Exception ex)
            { Console.WriteLine(ex.Message.ToString()); }
            Console.WriteLine("------------------");
            
            try
            {
                string ActivityWriteLine = @"
<Activity 
x:Class=""WFTemplate"" 
xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities"" 
xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">  
<Sequence>
<WriteLine Text=""HelloWorld"" /> 
</Sequence>
</Activity>";
                var act4 = CoreWf.XamlIntegration.ActivityXamlServices.Load(GenerateStreamFromString(ActivityWriteLine), settings);
                WorkflowInvoker.Invoke(act4);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            try
            {
                string ActivityWriteLine = @"
<Activity 
x:Class=""WFTemplate"" 
xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities"" 
xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">  
<Sequence>
<WriteLine><Content><Text>""HelloWorld""</Text></Content> </WriteLine>
</Sequence>
</Activity>";
                var act5 = CoreWf.XamlIntegration.ActivityXamlServices.Load(GenerateStreamFromString(ActivityWriteLine), settings);
                WorkflowInvoker.Invoke(act5);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            try
            {
                string ActivityAlone = @"
<Activity
 xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Sequence>
    <Sequence.Variables>
      <Variable x:TypeArguments=""x:String"" Default=""My variable text"" Name=""MyVar"" />
    </Sequence.Variables>
    <WriteLine Text=""[MyVar]"" />
  </Sequence>
</Activity>
";
                var act6 = CoreWf.XamlIntegration.ActivityXamlServices.Load(GenerateStreamFromString(ActivityAlone), settings);
                WorkflowInvoker.Invoke(act6);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            try
            {
                string ActivityWriteLine = @"
            <Activity 
            x:Class=""WFTemplate"" 
            xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities"" 
            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">  
            <WriteLine Text=""HelloWorld"" /> 
            </Activity>";
                var act7 = CoreWf.XamlIntegration.ActivityXamlServices.Load(GenerateStreamFromString(ActivityWriteLine), settings);
                WorkflowInvoker.Invoke(act7);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            try
            {
                string InOutActivityOnly = @"
            <Activity 
            x:Class=""WFTemplate""  
            xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""  
            xmlns:s=""clr-namespace:System;assembly=mscorlib""  
            xmlns:s1=""clr-namespace:System;assembly=System""  
            xmlns:sa=""clr-namespace:CoreWf;assembly=CoreWf""  
            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">   
            <x:Members>     
            <x:Property Name=""myOutput"" Type=""OutArgument(x:Int32)"" />     
            <x:Property Name=""myInput"" Type=""InArgument(x:Int32)"" />   
            </x:Members> </Activity>";
                var act8 = CoreWf.XamlIntegration.ActivityXamlServices.Load(GenerateStreamFromString(InOutActivityOnly), settings);
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic.Add("myInput", 1);
                Dictionary<string, object> outputDictP = WorkflowInvoker.Invoke(act8, dic) as Dictionary<string, object>;
                foreach (var kvp in outputDictP.ToList())
                {
                    Console.WriteLine(kvp.Key.ToString() + " " + kvp.Value.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            //try { 
            string InOutActivity = @"
            <Activity x:Class=""WFTemplate""  
            xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""  
            xmlns:s=""clr-namespace:System;assembly=mscorlib""  
            xmlns:s1=""clr-namespace:System;assembly=System""  
            xmlns:sa=""clr-namespace:CoreWf;assembly=CoreWf""  
            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">   
            <x:Members>     
            <x:Property Name=""myOutput"" Type=""OutArgument(x:Int32)"" />     
            <x:Property Name=""myInput"" Type=""InArgument(x:Int32)"" />   
            </x:Members>   
            <Assign>     
            <Assign.To> <OutArgument x:TypeArguments=""x:Int32"">[myOutput]</OutArgument> </Assign.To>     
            <Assign.Value>       <InArgument x:TypeArguments=""x:Int32"">[myInput]</InArgument>     </Assign.Value>   
            </Assign> 
            </Activity>";
            var act9 = CoreWf.XamlIntegration.ActivityXamlServices.Load(GenerateStreamFromString(InOutActivity), settings);
            var outputDict = WorkflowInvoker.Invoke(act9) as Dictionary<string, object>;
            foreach (var kvp in outputDict)
            {
                Console.WriteLine(kvp.Key.ToString() + " " + kvp.Value.ToString());
            }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}
           
        }
    }
}