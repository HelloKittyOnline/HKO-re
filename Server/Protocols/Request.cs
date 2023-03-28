using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Server.Protocols;

[AttributeUsage(AttributeTargets.Method)]
class Request : Attribute {
    private readonly int _major;
    private readonly int _minor;

    public delegate void ReceiveFunction(Client client);

    public Request(byte major, byte minor) {
        _major = major;
        _minor = minor;
    }

    public static Dictionary<int, ReceiveFunction> GetEndpoints() {
        var assemlby = Assembly.GetAssembly(typeof(Request));

        var functions = new Dictionary<int, ReceiveFunction>();

        foreach(var method in assemlby.GetTypes().SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))) {
            var att = method.GetCustomAttribute<Request>();
            if(att == null)
                continue;

            var key = (att._major << 8) | att._minor;
            functions[key] = method.CreateDelegate<ReceiveFunction>();
        }

        return functions;
    }
}
