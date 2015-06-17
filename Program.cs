using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace JuggleFest
{
    abstract class HEP
    {
        public string Name = String.Empty;
        public uint H = 0; //hand to eye coordination
        public uint E = 0; //endurance
        public uint P = 0; //pizzazz

        protected static void ParseParameter(object obj, string[] param)
        {
            if (param.Length < 2 || !(param[0] == "C" || param[0] == "J")) return;
            System.Collections.IEnumerator list = param.GetEnumerator();
            while (list.MoveNext())
            {
                string str = list.Current as String;
                if (str.Equals("C") || str.Equals("J"))
                {
                    list.MoveNext();
                    obj.GetType().GetField("Name").SetValue(obj, list.Current);
                    continue;
                }
                string[] par = str.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                FieldInfo fi = obj.GetType().BaseType.GetField(par[0]);
                if (fi != null) fi.SetValue(obj, UInt32.Parse(par[1]));
            }
        }
    }

    class Circuit : HEP
    {
        public IDictionary<Juggler, uint> Jugglers = new Dictionary<Juggler, uint>();

        public static IDictionary<string,Circuit> CircuitCollection = new Dictionary<string,Circuit>();
        public static Circuit Parse(string[] features)
        {
            Circuit tmp = new Circuit();
            HEP.ParseParameter(tmp, features);
            return tmp;
        }
    }

    class Juggler : HEP
    {
        public IDictionary<string,uint> PreferenceCollection = new Dictionary<string, uint>();
        public override string ToString()
        {
            return this.Name;
        }

        public static IList<Juggler> JugglerCollection = new List<Juggler>();
        public static IList<Juggler> MisfitCollection = new List<Juggler>();
        public static Juggler Parse(string[] features)
        {
            Juggler tmp = new Juggler();
            HEP.ParseParameter(tmp, features);
            //Parse preferences
            if (features.Length >= 6)
            {
                string[] pref = features[5].Split(new char[]{','});
                foreach (string p in pref)
                {
                    Circuit c = Circuit.CircuitCollection[p]; 
                    uint tot =  c.E * tmp.E + 
                                c.H * tmp.H +
                                c.P * tmp.P;
                    tmp.PreferenceCollection.Add(p, tot);
                }
            }
            return tmp;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            System.IO.StreamReader input = new System.IO.StreamReader(@"jugglefest.txt");
            System.IO.StreamWriter output = new System.IO.StreamWriter(@"jugglefestout.txt");
            
            ParseInput(input);
            DistributeParticipants();
            FormatAndOutput(output);
            ShowAnswer();
            
            input.Close();
            output.Close();
            Console.Write("Press any key to exit");
            Console.ReadKey();
        }

        static void ParseInput(System.IO.StreamReader input)
        {
            string line = String.Empty;
            while ((line = input.ReadLine()) != null)
            {
                if (line == String.Empty) continue;
                string[] str = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (str.Length < 5) continue;
                if (str.First() == "C")
                {
                    Circuit tmp = Circuit.Parse(str);
                    Circuit.CircuitCollection.Add(tmp.Name, tmp);
                }
                else if (str.First() == "J")
                    Juggler.JugglerCollection.Add(Juggler.Parse(str));
            }
        }

        static void DistributeParticipants()
        {
            int roomSpace = Juggler.JugglerCollection.Count / Circuit.CircuitCollection.Count;
            Queue<Juggler> candidates = new Queue<Juggler>(Juggler.JugglerCollection);
            while (candidates.Count > 0)
            {
                Juggler j = candidates.Peek();
                foreach (var p in j.PreferenceCollection)
                {
                    IDictionary<Juggler, uint> jRoom = Circuit.CircuitCollection[p.Key].Jugglers;
                    if (jRoom.Count < roomSpace)
                    {
                        jRoom.Add(candidates.Dequeue(), p.Value);
                        break;
                    }
                    else if (p.Value > jRoom.Values.Min())
                    {
                        Juggler uj = jRoom.OrderBy(v => v.Value).First().Key;
                        candidates.Enqueue(uj);
                        jRoom.Remove(uj);
                        jRoom.Add(candidates.Dequeue(), p.Value);
                        break;
                    }
                }
                if (candidates.Count > 0 && j == candidates.Peek())
                    Juggler.MisfitCollection.Add(candidates.Dequeue());
            }
            //and sort them by score
            int tot = 0;
            System.Diagnostics.Debug.WriteLine("\n");
            foreach (var c in Circuit.CircuitCollection)
            {
                c.Value.Jugglers = c.Value.Jugglers
                    .OrderByDescending(kv => kv.Value)
                    .ToDictionary(t => t.Key, t => t.Value);
                tot += c.Value.Jugglers.Count;
                System.Diagnostics.Debug.WriteIf(c.Value.Jugglers.Count<6,String.Format("{0}:{1} ", c.Key, c.Value.Jugglers.Count));
            }
            System.Diagnostics.Debug.WriteLine("\n");
            System.Diagnostics.Debug.WriteLine(tot);
        }

        static void FormatAndOutput(System.IO.StreamWriter output)
        {
            var l = Juggler.JugglerCollection;
            foreach (var c in Circuit.CircuitCollection)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} ", c.Key);
                foreach (var j in c.Value.Jugglers)
                {
                    sb.AppendFormat("{0} ", j.Key.Name);
                    foreach (var p in j.Key.PreferenceCollection)
                        sb.AppendFormat("{0}:{1} ", p.Key, p.Value);
                    sb.Insert(sb.Length - 1, ',');
                }
                string str = sb.ToString().Trim();
                str = str.TrimEnd(new char[]{','});
                output.WriteLine(str);
            }
            output.Flush();
        }

        static void ShowAnswer()
        {
            if (Circuit.CircuitCollection.Keys.Contains("C1970"))
            {
                uint total = 0;
                foreach (var j in Circuit.CircuitCollection["C1970"].Jugglers)
                    total += UInt32.Parse(j.Key.Name.ToString().Substring(1));
                Console.WriteLine("Address is {0}@juggle.com", total);
            }
            if (Juggler.MisfitCollection.Count > 0)
                Console.WriteLine("There are {0} unallocated jugglers", Juggler.MisfitCollection.Count);
        }
    }
}
