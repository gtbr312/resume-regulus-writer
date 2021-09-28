using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace RegulusWriter
{
    class Program
    {
        public class Endpoint
        {
            public string name;
            public List<Endpoint> children;
            public string model;
            public Endpoint(string name, string model, List<Endpoint> children)
            {
                this.name = name;
                this.model = model;
                this.children = children;
            }
        }
        static int indentCount = 1;
        static string indent = "    ";

        static string getIndent()
        {
            string fullIndent = "";
            for(int i = 0; i < indentCount; i++)
            {
                fullIndent = fullIndent + indent;
            }
            return fullIndent;
        }
        static void Main(string[] args)
        {
            string frontEndOutputBase = $"./regulus/src/redux/";
            List<Endpoint> runningRoutes = new List<Endpoint>();

            //Endpoint bigAreas = new Endpoint("/big-Areas", "BigArea", new List<Endpoint>());
            //runningRoutes.Add(bigAreas);

            //Endpoint tinyAreas = new Endpoint("/tiny-Areas", "TinyArea", new List<Endpoint>());
            //Endpoint smallAreas = new Endpoint("/smell-areas", "SmallArea", new List<Endpoint> { tinyAreas });
            //runningRoutes.Add(smallAreas);

            
            Endpoint location = new Endpoint("locations", "Location", new List<Endpoint> { });
            runningRoutes.Add(location);
            
            string backendOutPutFile = $"./RegulusBackend/socket-routes/{runningRoutes[runningRoutes.Count - 1].name}Router.js";
            StreamWriter file = new StreamWriter(backendOutPutFile);
            WriteImports(location);
            WriteStoreImports(runningRoutes);
            WriteRoutes(runningRoutes, file);
            WriteReducer(location, "");

            runningRoutes.Clear();

            

            Endpoint employees = new Endpoint("employees", "Employee", new List<Endpoint> { });
            runningRoutes.Add(employees);

            backendOutPutFile = $"./RegulusBackend/socket-routes/{runningRoutes[runningRoutes.Count - 1].name}Router.js";
            file = new StreamWriter(backendOutPutFile);
            WriteImports(employees);
            WriteStoreImports(runningRoutes);
            WriteRoutes(runningRoutes, file);
            WriteReducer(employees, "");

            /*
            Endpoint employees = new Endpoint("employees", "Location", new List<Endpoint> { });
            runningRoutes.Add(employees);
            Endpoint workers = new Endpoint("workers", "Location", new List<Endpoint> { employees });
            runningRoutes.Add(workers);

            backendOutPutFile = $"./RegulusBackend/socket-routes/{runningRoutes[runningRoutes.Count - 1].name}Router.js";
            file = new StreamWriter(backendOutPutFile);

            WriteImports(workers);
            WriteRoutes(runningRoutes, file);
            WriteReducer(workers, "");
            */

        }

        static void WriteStoreImports(List<Endpoint> stores)
        {
            System.Text.StringBuilder currentContent = new System.Text.StringBuilder();
            List<string> rawList = File.ReadAllLines("./regulus/src/redux/rootReducer.js").ToList();
            
            foreach(Endpoint store in stores)
            {
                string name = store.name;

                if(name[0] == '/')
                {
                    name = name.Substring(1);
                }

                currentContent.Append($"import {name}Reducer from './{name}/{name}.reducer.js'" + Environment.NewLine);
            }
            
            foreach (var item in rawList)
            {
                currentContent.Append(item + Environment.NewLine);
                if(item.Contains("//do not delete"))
                {
                    foreach(Endpoint store in stores)
                    {
                        string name = store.name;

                        if (name[0] == '/')
                        {
                            name = name.Substring(1);
                        }
                        currentContent.Append($"    {name} : {name}Reducer," + Environment.NewLine);
                    }

                }
            }
            File.WriteAllText("./regulus/src/redux/rootReducer.js", currentContent.ToString());
        }

        static void WriteImports(Endpoint node)
        {
            string name = node.name;

            if (name[0] == '/')
            {
                name = name.Substring(1);
            }
            System.Text.StringBuilder currentContent = new System.Text.StringBuilder();
                List<string> rawList = File.ReadAllLines("./RegulusBackend/App.js").ToList();
                currentContent.Append($"import './socket-routes/{name}Router.js'\n");
                foreach (var item in rawList)
                {
                    currentContent.Append(item + Environment.NewLine);
                }
                File.WriteAllText("./RegulusBackend/App.js", currentContent.ToString());
        }

        static void WriteReducer(Endpoint node, string runningRoute)
        {
            foreach (Endpoint child in node.children)
            {
                WriteReducer(child, runningRoute + node.name);
            }

            string route = "./regulus/src/redux/" + node.name;
            string name = node.name;
            if(name[0] == '/')
                name = node.name.Substring(1);
            
            name = name.Replace("-", "");


            if (!System.IO.Directory.Exists(route))
            {
                Directory.CreateDirectory(route);
            }

            StreamWriter file = new StreamWriter("./regulus/src/redux/" + node.name + "/" + node.name + ".reducer.js");
            using (file) { 
            file.WriteLine("import server from '../../SocketProtocal'");
                file.WriteLine("");
                file.WriteLine($"const INITIAL_STATE = {{ {name}:{{}}, pageNumber:0, listSize:150 }}");
                file.WriteLine("");
                file.WriteLine($"const {name}Reducer = (state = INITIAL_STATE, {{type, payload}}) => {{");
                file.WriteLine("");

                file.WriteLine("    switch(type){");

                file.WriteLine($"        case 'CHECK_UPDATED_{name.ToUpper().Substring(0, name.Length - 1)}':{{");
                file.WriteLine($"           if(state.locations[payload._id]){{");
                file.WriteLine($"               server.fetch('/{runningRoute + node.name}, {{_id: payload._id}}')");
                file.WriteLine("           }");
                file.WriteLine("            return state;");
                file.WriteLine("        }");

                file.WriteLine($"        case 'CHECK_NEW_{name.ToUpper().Substring(0, name.Length - 1)}':{{");
                file.WriteLine($"           server.fetch('/{runningRoute + node.name}', payload)");
                file.WriteLine("            return state;");
                file.WriteLine("        }");

                file.WriteLine($"       case 'INCOMING_{name.ToUpper().Substring(0, name.Length - 1)}':{{");
                file.WriteLine("            const data = {}");
                file.WriteLine("            data[payload[0]._id] = payload[0];");
                file.WriteLine("            if(state[payload[0]._id]){");
                file.WriteLine($"                state = {{...state, {name}:{{...state.locations, ...data}}}}");
                file.WriteLine("            } else {");
                file.WriteLine("                state = { ...data, ...state}");
                file.WriteLine("            }");

                file.WriteLine("            return state;");
                file.WriteLine("        }");

                file.WriteLine($"        case 'CHECK_REMOVED_{name.ToUpper().Substring(0, name.Length - 1)}':{{");
                file.WriteLine("               if(state.locations[payload]){");
                file.WriteLine("                    delete state.locations[payload];");
                file.WriteLine("                    return {...state}");
                file.WriteLine("                }");
                file.WriteLine("                return state;");
                file.WriteLine("        }");

                file.WriteLine($"        case 'INCOMING_DATASET_{name.ToUpper().Substring(0, name.Length - 1)}':{{");
                file.WriteLine("               const data = {}");
                file.WriteLine("               payload.forEach(doc => data[doc._id] = doc)");
                file.WriteLine("               return {...state, ...data}");
                file.WriteLine("        }");


                file.WriteLine("        default: return state");
                file.WriteLine("    }");

                file.WriteLine("");
                file.WriteLine("}");
                file.WriteLine($"export default {name}Reducer;");
            }
        }

        static void WriteRoutes(List<Endpoint> router, StreamWriter file)
        {

            using (file)
            {
                file.WriteLine("import sockets from '../SocketProtocal.js'");
                foreach (Endpoint node in router)
                {
                    file.WriteLine($"import {node.model} from '../models/{node.model}.js'");
                }
                file.WriteLine();
                file.WriteLine();
                file.WriteLine();


                router.Reverse();
                Endpoint baseNode = router[0];

                    if(baseNode.children.Count > 0)
                    {
                        file.WriteLine($"    sockets.use('{baseNode.name}', {baseNode.model}, () => {{");
                        foreach(Endpoint childNode in baseNode.children)
                            WriteEndpoint(childNode, (string ln) => { file.WriteLine(ln); return ln; });
                        file.WriteLine($"    }});");

                    }else
                        file.WriteLine($"    sockets.use('{baseNode.name}', {baseNode.model})");
                
                

                
            }
        }

        static void WriteEndpoint(Endpoint node, Func<string, string> write)
        {
            indentCount++;
            if (node.children.Count < 1)
                write(getIndent() + $"sockets.use('{node.name}', {node.model})");
            else {
                write(getIndent() + $"sockets.use('{node.name}', {node.model}, () => {{");
                foreach (Endpoint childNode in node.children)
                    WriteEndpoint(childNode, write);
                write(getIndent() + $"}});");
            }
            indentCount--;
        }
    }
}
