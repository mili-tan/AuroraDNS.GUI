/*
------------------------------------------------------------------------------------------------------------------------
 * Copyright (c) 2017-2018 scott.cgi All Rights Reserved.
 * 
 * This code is licensed under the MIT License.
 *
 * Since  : 2017-9-6
 * Author : scott.cgi
 * Version: 1.1.8
 * Update : 2018-2-5
------------------------------------------------------------------------------------------------------------------------
 */

using System.Collections.Generic;
using System.Text;
using System;

// ReSharper disable once CheckNamespace
namespace MojoUnity
{
    public static class Json 
    {
        private const int JsonObjectInitCapacity = 8;
        private const int JsonArrayInitCapacity  = 8;

        #region Parse Json API

        /// <summary>
        /// Parse json string.
        /// </summary>
        public static JsonValue Parse(string json)
        {
            return ParseValue(new Data(json));
        }

        #endregion


        #region Parse Json 

        /// <summary>
        /// Parse the JsonValue.
        /// </summary>
        private static JsonValue ParseValue(Data data)
        {
            SkipWhiteSpace(data);

            switch (data.json[data.index])
            {
                case '{':
                    return ParseObject(data);

                case '[':
                    return ParseArray (data);

                case '"':
                    return ParseString(data);

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    return ParseNumber(data);

                case 'f':
                    if 
                    (
                        data.json[data.index + 1] == 'a' && 
                        data.json[data.index + 2] == 'l' &&
                        data.json[data.index + 3] == 's' &&
                        data.json[data.index + 4] == 'e'
                    )
                    {
                        data.index += 5;
                        return new JsonValue(JsonType.Bool, 0.0f);
                    }
                    break;

                case 't':
                    if 
                    (
                        data.json[data.index + 1] == 'r' && 
                        data.json[data.index + 2] == 'u' &&
                        data.json[data.index + 3] == 'e'
                    )
                    {
                        data.index += 4;
                        return new JsonValue(JsonType.Bool, 1.0f);
                    }
                    break;

                case 'n':
                    if 
                    (
                        data.json[data.index + 1] == 'u' && 
                        data.json[data.index + 2] == 'l' &&
                        data.json[data.index + 3] == 'l'
                    )
                    {
                        data.index += 4;
                        return new JsonValue(JsonType.Null, null);
                    }
                    break;
            }

            throw new Exception
            (
                string.Format
                (
                    "Json ParseValue error on char '{0}' index at '{1}' ", 
                    data.json[data.index], 
                    data.index
                )
            );
        }


        /// <summary>
        /// Parse JsonObject.
        /// </summary>
        private static JsonValue ParseObject(Data data)
        {
            var jsonObject = new Dictionary<string, JsonValue>(JsonObjectInitCapacity);

            // skip '{'
            data.index++;

            do
            {
                SkipWhiteSpace(data);

                if (data.json[data.index] == '}')
                {
                    break;
                }

                DebugTool.Assert
                (
                    data.json[data.index] == '"',  
                    "Json ParseObject error, char '{0}' should be '\"' ", 
                    data.json[data.index]
                );

                // skip '"'
                data.index++;

                var start = data.index;

                while (true)
                {
                    switch (data.json[data.index++])
                    {
                        // check end '"'
                        case '"':
                            break;

                        case '\\':
                            // skip escaped quotes
                            data.index++;
                            continue;

                        default:
                            continue;
                    }

                    // already skip the end '"'
                    break;
                }

                // get object key string
                var key = data.json.Substring(start, data.index - start - 1);

                SkipWhiteSpace(data);

                DebugTool.Assert
                (
                    data.json[data.index] == ':',  
                    "Json ParseObject error, after key = {0}, char '{1}' should be ':' ", 
                    key,
                    data.json[data.index]
                );
            
                // skip ':'
                data.index++;

                // set JsonObject key and value
                jsonObject.Add(key, ParseValue(data));

                SkipWhiteSpace(data);

                if (data.json[data.index] == ',')
                {
                    data.index++;                   
                }
                else
                {
                    DebugTool.Assert
                    (
                        data.json[data.index] == '}',  
                        "Json ParseObject error, after key = {0}, char '{1}' should be '}' ",
                        key,
                        data.json[data.index]
                    );

                    break;
                }
            }
            while (true);

            // skip '}' and return after '}'
            data.index++;

            return new JsonValue(JsonType.Object, jsonObject);
        }


        /// <summary>
        /// Parse JsonArray.
        /// </summary>
        private static JsonValue ParseArray(Data data)
        {
            var jsonArray = new List<JsonValue>(JsonArrayInitCapacity);

            // skip '['
            data.index++;

            do
            {
                SkipWhiteSpace(data);

                if (data.json[data.index] == ']')
                {
                    break;
                }

                // add JsonArray item 
                jsonArray.Add(ParseValue(data));

                SkipWhiteSpace(data);

                if (data.json[data.index] == ',')
                {
                    data.index++;
                }
                else
                {
                    DebugTool.Assert
                    (
                        data.json[data.index] == ']',  
                        "Json ParseArray error, char '{0}' should be ']' ", 
                        data.json[data.index]
                    );
                    break;
                }
            }
            while (true);

            // skip ']'
            data.index++;

            return new JsonValue(JsonType.Array, jsonArray);
        }


        /// <summary>
        /// Parses the JsonString.
        /// </summary>
        private static JsonValue ParseString(Data data)
        {
            // skip '"'
            data.index++;

            var    start = data.index;
            string str;

            while (true)
            {
                switch (data.json[data.index++])
                {
                    // check string end '"' 
                    case '"':
                        if (data.sb.Length == 0)
                        {
                            // no escaped char just Substring
                            str = data.json.Substring(start, data.index - start - 1);
                        }
                        else
                        {
                            str = data.sb.Append(data.json, start, data.index - start - 1).ToString();
                            // clear for next string
                            data.sb.Length = 0;
                        }
                        break;

                    // check escaped char
                    case '\\':  
                    {
                        var  escapedIndex = data.index;
                        char c;

                        switch (data.json[data.index++])
                        {
                            case '"':
                                c = '"';
                                break;

                            case '\\':
                                c = '\\';
                                break;

                            case '/':
                                c = '/';
                                break;

                            case '\'':
                                c = '\'';
                                break;

                            case 'b':
                                c = '\b';
                                break;

                            case 'f':
                                c = '\f';
                                break;

                            case 'n':
                                c = '\n';
                                break;

                            case 'r':
                                c = '\r';
                                break;

                            case 't':
                                c = '\t';
                                break;

                            case 'u':
                                c = GetUnicodeCodePoint
                                    (
                                        data.json[data.index], 
                                        data.json[data.index + 1], 
                                        data.json[data.index + 2], 
                                        data.json[data.index + 3]
                                    );

                                // skip code point
                                data.index += 4;
                                break;

                            default:
                                // not support just add in pre string
                                continue;
                        }

                        // add pre string and escaped char
                        data.sb.Append(data.json, start, escapedIndex - start - 1).Append(c);

                        // update pre string start index
                        start = data.index;
                        continue;
                    }

                    default:
                        continue;
                }

                // already skip the end '"'
                break;
            }

            return new JsonValue(JsonType.String, str);
        }


        /// <summary>
        /// Parses the JsonNumber.
        /// </summary>
        private static JsonValue ParseNumber(Data data)
        {
            var start = data.index;
                
            while (true)
            {
                switch (data.json[++data.index])
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                    case '+':
                    case '.':
                    case 'e':
                    case 'E':
                        continue;
                }

                break;
            }

            var   strNum = data.json.Substring(start, data.index - start);

            if (float.TryParse(strNum, out var num))
            {
                return new JsonValue(JsonType.Number, num);
            }
            else
            {
                throw new Exception(string.Format("Json ParseNumber error, can not parse string [{0}]", strNum));
            }
        }


        /// <summary>
        /// Skip the white space.
        /// </summary>
        private static void SkipWhiteSpace(Data data)
        {
            while (true)
            {
                switch (data.json[data.index])
                {
                    case ' ' :
                    case '\t':
                    case '\n':
                    case '\r':
                        data.index++;
                        continue;
                }

                break;
            }
        }


        /// <summary>
        /// Get the unicode code point.
        /// </summary>
        private static char GetUnicodeCodePoint(char c1, char c2, char c3, char c4)
        {
            return (char)
                   (
                       UnicodeCharToInt(c1) * 0x1000 +
                       UnicodeCharToInt(c2) * 0x100  +
                       UnicodeCharToInt(c3) * 0x10   +
                       UnicodeCharToInt(c4)
                   );
        }


        /// <summary>
        /// Single unicode char convert to int.
        /// </summary>
        private static int UnicodeCharToInt(char c)
        {
            switch (c)
            {
                case '0':
                    return 0;

                case '1':
                    return 1;

                case '2':
                    return 2;

                case '3':
                    return 3;

                case '4':
                    return 4;

                case '5':
                    return 5;

                case '6':
                    return 6;

                case '7':
                    return 7;

                case '8':
                    return 8;

                case '9':
                    return 9;

                case 'A':
                case 'a':
                    return 10;

                case 'B':
                case 'b':
                    return 11;

                case 'C':
                case 'c':
                    return 12;

                case 'D':
                case 'd':
                    return 13;

                case 'E':
                case 'e':
                    return 14;

                case 'F':
                case 'f':
                    return 15;
            }

            throw new Exception(string.Format("Json Unicode char '{0}' error", c));
        }


        #endregion


//----------------------------------------------------------------------------------------------------------------------


        private class Data
        {
            public string        json;
            public int           index;
            public StringBuilder sb;


            public Data(string json)
            {
                this.json  = json;
                this.index = 0;
                this.sb    = new StringBuilder();
            }
        }
    }


//----------------------------------------------------------------------------------------------------------------------


    public enum JsonType
    {
        Object,
        Array,
        String,
        Number,
        Bool,
        Null,
    }


//----------------------------------------------------------------------------------------------------------------------


    public class JsonValue
    {
        public  readonly JsonType type;
        private object   objectValue;
        private float    numberValue;


        public JsonValue(JsonType type, object value)
        {
            this.type        = type;
            this.objectValue = value;
        }


        public JsonValue(JsonType type, float value)
        {
            this.type        = type;
            this.numberValue = value;
        }


//----------------------------------------------------------------------------------------------------------------------


        #region JsonObject API


        /// <summary>
        /// Use JsonValue as JsonObject.
        /// </summary>
        public Dictionary<string, JsonValue> AsObject()
        {
            DebugTool.Assert(this.type == JsonType.Object, "JsonValue type is not Object !");
            return this.objectValue as Dictionary<string, JsonValue>;
        }


        /// <summary>
        /// Use JsonValue as JsonObject and get JsonValue item by key.
        /// return null if not found key.
        /// </summary>
        public JsonValue AsObjectGet(string key)
        {
            DebugTool.Assert(this.type == JsonType.Object, "JsonValue type is not Object !");
            var dict = this.objectValue as Dictionary<string, JsonValue>;

            if (dict.TryGetValue(key, out var jsonValue))
            {
                return jsonValue;
            }
            else 
            {
                return null;
            }
        }


        /// <summary>
        /// Use JsonValue as JsonObject and get JsonObject item by key.
        /// return null if not found key.
        /// </summary>
        public Dictionary<string, JsonValue> AsObjectGetObject(string key)
        {
            var jsonValue = this.AsObjectGet(key);

            if (jsonValue != null)
            {
                return jsonValue.AsObject();
            }
            else 
            {
                return null;               
            }
        }


        /// <summary>
        /// Use JsonValue as JsonObject and get JsonArray item by key.
        /// return null if not found key.
        /// </summary>
        public List<JsonValue> AsObjectGetArray(string key)
        {
            var jsonValue = this.AsObjectGet(key);

            if (jsonValue != null)
            {
                return jsonValue.AsArray();
            }
            else 
            {
                return null;               
            }
        }


        /// <summary>
        /// Use JsonValue as JsonObject and get string item by key.
        /// return null if not found key.
        /// </summary>
        public string AsObjectGetString(string key)
        {
            var jsonValue = this.AsObjectGet(key);

            if (jsonValue != null)
            {
                return jsonValue.AsString();
            }
            else 
            {
                return null;               
            }
        }


        /// <summary>
        /// Use JsonValue as JsonObject and get float item by key.
        /// return defaultValue if not found key.
        /// </summary>
        public float AsObjectGetFloat(string key, float defaultValue)
        {
            var jsonValue = this.AsObjectGet(key);

            if (jsonValue != null)
            {
                return jsonValue.AsFloat();
            }
            else 
            {
                return defaultValue;               
            }
        }


        /// <summary>
        /// Use JsonValue as JsonObject and get float item by key.
        /// </summary>
        public float AsObjectGetFloat(string key)
        {
            return this.AsObjectGet(key).AsFloat();
        }


        /// <summary>
        /// Use JsonValue as JsonObject and get int item by key.
        /// return defaultValue if not found key.
        /// </summary>
        public int AsObjectGetInt(string key, int defaultValue)
        {
            var jsonValue = this.AsObjectGet(key);

            if (jsonValue != null)
            {
                return jsonValue.AsInt();
            }
            else 
            {
                return defaultValue;               
            }
        }


        /// <summary>
        /// Use JsonValue as JsonObject and get int item by key.
        /// </summary>
        public int AsObjectGetInt(string key)
        {
            return this.AsObjectGet(key).AsInt();
        }


        /// <summary>
        /// Use JsonValue as JsonObject and get bool item by key.
        /// return defaultValue if not found key.
        /// </summary>
        public bool AsObjectGetBool(string key, bool defaultValue)
        {
            var jsonValue = this.AsObjectGet(key);

            if (jsonValue != null)
            {
                return jsonValue.AsBool();
            }
            else 
            {
                return defaultValue;               
            }
        }


        /// <summary>
        /// Use JsonValue as JsonObject and get int item by key.
        /// </summary>
        public bool AsObjectGetBool(string key)
        {
            return this.AsObjectGet(key).AsBool();
        }


        /// <summary>
        /// Use JsonValue as JsonObject and check null item by key.
        /// </summary>
        public bool AsObjectGetIsNull(string key)
        {
            var jsonValue = this.AsObjectGet(key);

            if (jsonValue != null)
            {
                return jsonValue.IsNull();
            }
            else 
            {
                return false;               
            }
        }


        #endregion


//----------------------------------------------------------------------------------------------------------------------


        #region JsonArray API


        /// <summary>
        /// Use JsonValue as JsonArray.
        /// </summary>
        public List<JsonValue> AsArray()
        {
            DebugTool.Assert(this.type == JsonType.Array, "JsonValue type is not Array !");
            return this.objectValue as List<JsonValue>;
        }


        /// <summary>
        /// Use JsonValue as JsonArray and get JsonValue item by index.
        /// </summary>
        public JsonValue AsArrayGet(int index)
        {
            DebugTool.Assert(this.type == JsonType.Array, "JsonValue type is not Array !");
            return (this.objectValue as List<JsonValue>)[index];
        }


        /// <summary>
        /// Use JsonValue as JsonArray and get JsonObject item by index.
        /// </summary>
        public Dictionary<string, JsonValue> AsArrayGetObject(int index)
        {
            return this.AsArrayGet(index).AsObject();
        }


        /// <summary>
        /// Use JsonValue as JsonArray and get JsonArray item by index.
        /// </summary>
        public List<JsonValue> AsArrayGetArray(int index)
        {
            return this.AsArrayGet(index).AsArray();
        }


        /// <summary>
        /// Use JsonValue as JsonArray and get string item by index.
        /// </summary>
        public string AsArrayGetString(int index)
        {
            return this.AsArrayGet(index).AsString();
        }


        /// <summary>
        /// Use JsonValue as JsonArray and get float item by index.
        /// </summary>
        public float AsArrayGetFloat(int index)
        {
            return this.AsArrayGet(index).AsFloat();
        }


        /// <summary>
        /// Use JsonValue as JsonArray and get int item by index.
        /// </summary>
        public int AsArrayGetInt(int index)
        {
            return this.AsArrayGet(index).AsInt();
        }


        /// <summary>
        /// Use JsonValue as JsonArray and get bool item by index.
        /// </summary>
        public bool AsArrayGetBool(int index)
        {
            return this.AsArrayGet(index).AsBool();
        }


        /// <summary>
        /// Use JsonValue as JsonArray and check null item by index.
        /// </summary>
        public bool AsArrayGetIsNull(int index)
        {
            return this.AsArrayGet(index).IsNull();
        }


        #endregion


//----------------------------------------------------------------------------------------------------------------------


        #region Other Json value API


        /// <summary>
        /// Get JsonValue as string.
        /// </summary>
        public string AsString()
        {
            DebugTool.Assert(this.type == JsonType.String, "JsonValue type is not String !");
            return this.objectValue as string;
        }


        /// <summary>
        /// Get JsonValue as float.
        /// </summary>
        public float AsFloat()
        {
            DebugTool.Assert(this.type == JsonType.Number, "JsonValue type is not Number !");
            return this.numberValue;
        }


        /// <summary>
        /// Get JsonValue as int.
        /// </summary>
        public int AsInt()
        {
            DebugTool.Assert(this.type == JsonType.Number, "JsonValue type is not Number !");
            return (int) this.numberValue;
        }


        /// <summary>
        /// Get JsonValue as bool.
        /// </summary>
        public bool AsBool()
        {
            DebugTool.Assert(this.type == JsonType.Bool, "JsonValue type is not Bool !");
            return this.numberValue > 0.0f;
        }


        /// <summary>
        /// Whether JsonValue is null ？
        /// </summary>
        public bool IsNull()
        {
            return this.type == JsonType.Null;
        }


        #endregion
    }


//----------------------------------------------------------------------------------------------------------------------


    internal static class DebugTool
    {
        public static void Assert(bool condition, string msg, params object[] args)
        {
            if (condition == false)
            {
                throw new Exception(string.Format(msg, args));
            }
        }
    }
}

