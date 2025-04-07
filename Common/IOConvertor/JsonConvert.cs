using System;
using System.Collections.Generic;
using System.Windows;
using System.Text;
using System.Json;

namespace Presentation
{
    public static class JsonConvert
    {
        static List<string> errors = new List<string>();
        public static void ClearErrors() { errors.Clear(); }
        public static void AddError(string err) { errors.Add(err); }
        public static string[] GetErrors() { return errors.ToArray(); }
        public static JsonValue ToValue(JsonValue jv, string key)
        {
            JsonValue v = jv.ContainsKey(key) ? jv[key] : null;
            string error = v == null ? "Key '" + key + "' not found" : "";
            if (error.Length != 0) errors.Add(error);
            return v;
        }
        public static int ToInt(JsonValue jv, string key)
        {
            if (jv == null) return 0;
            JsonValue v = jv.ContainsKey(key) ? jv[key] : null;
            string error = v == null ? "Key '" + key + "' not found" : v.JsonType != JsonType.Number ? "Key " + key + " not a number" : "";
            if (error.Length == 0) return (int)v;
            errors.Add(error);
            return 0;
        }
        public static double ToDouble(JsonValue jv, string key)
        {
            if (jv == null) return 0;
            JsonValue v = jv.ContainsKey(key) ? jv[key] : null;
            string error = v == null ? "Key '" + key + "' not found" : v.JsonType != JsonType.Number ? "Key " + key + " not a number" : "";
            if (error.Length == 0) return (double)v;
            errors.Add(error);
            return 0;
        }
        public static string ToString(JsonValue jv, string key)
        {
            if (jv == null) return "";
            JsonValue v = jv.ContainsKey(key) ? jv[key] : null;
            string error = v == null ? "Key '" + key + "' not found" : v.JsonType != JsonType.String ? "Key " + key + " not a string" : "";
            if (error.Length == 0) return (string)v;
            errors.Add(error);
            return "";
        }
    }
}
