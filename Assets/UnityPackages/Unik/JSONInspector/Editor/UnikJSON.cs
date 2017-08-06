using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Unik.JSON
{
    public class JSONNode
    {
        #region VIRTUAL INTERFACE

        public virtual JSONNode this[int index]
        {
            get { return null; }
            set { }
        }

        public virtual JSONNode this[string key]
        {
            get { return null; }
            set { }
        }

        public virtual string Value
        {
            get { return ""; }
            set { }
        }

        public virtual int Count
        {
            get { return 0; }
        }

        public virtual void Add(string key, JSONNode item)
        {
        }

        public virtual void Add(JSONNode item)
        {
            Add("", item);
        }

        public virtual JSONNode Remove(string key)
        {
            return null;
        }

        public virtual JSONNode Remove(int index)
        {
            return null;
        }

        public virtual JSONNode Remove(JSONNode node)
        {
            return node;
        }

        public virtual IEnumerable<JSONNode> Childs
        {
            get { yield break; }
        }

        public IEnumerable<JSONNode> DeepChilds
        {
            get { return Childs.SelectMany(child => child.DeepChilds); }
        }

        public override string ToString()
        {
            return "JSONNode";
        }

        #endregion

        #region TYPECASTING

        public virtual bool IsInt
        {
            get
            {
                int v;
                return int.TryParse(Value, out v);
            }
        }

        public virtual bool IsFloat
        {
            get
            {
                float v;
                return float.TryParse(Value, out v);
            }
        }

        public virtual bool IsDouble
        {
            get
            {
                double v;
                return double.TryParse(Value, out v);
            }
        }

        public virtual bool IsBool
        {
            get
            {
                bool v;
                return bool.TryParse(Value, out v);
            }
        }

        public virtual int AsInt
        {
            get
            {
                int v;
                return int.TryParse(Value, out v) ? v : 0;
            }
            set { Value = value.ToString(); }
        }

        public virtual float AsFloat
        {
            get
            {
                float v;
                return float.TryParse(Value, out v) ? v : 0.0f;
            }
            set { Value = value.ToString(); }
        }

        public virtual double AsDouble
        {
            get
            {
                double v;
                return double.TryParse(Value, out v) ? v : 0.0;
            }
            set { Value = value.ToString(); }
        }

        public virtual bool AsBool
        {
            get
            {
                bool v;
                if (bool.TryParse(Value, out v))
                    return v;
                return !string.IsNullOrEmpty(Value);
            }
            set { Value = (value) ? "true" : "false"; }
        }

        public virtual JSONArray AsArray
        {
            get { return this as JSONArray; }
        }

        public virtual JSONObject AsObject
        {
            get { return this as JSONObject; }
        }

        #endregion

        #region OPERATORS

        public static implicit operator JSONNode(string str)
        {
            return new JSONData(str);
        }

        public static implicit operator string(JSONNode node)
        {
            return (node == null) ? null : node.Value;
        }

        public static bool operator ==(JSONNode a, object b)
        {
            if (b == null && a is JSONLazyCreator)
                return true;
            return ReferenceEquals(a, b);
        }

        public static bool operator !=(JSONNode a, object b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        internal static string Escape(string text)
        {
            string result = "";
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\\':
                        result += "\\\\";
                        break;
                    case '\"':
                        result += "\\\"";
                        break;
                    case '\n':
                        result += "\\n";
                        break;
                    case '\r':
                        result += "\\r";
                        break;
                    case '\t':
                        result += "\\t";
                        break;
                    case '\b':
                        result += "\\b";
                        break;
                    case '\f':
                        result += "\\f";
                        break;
                    default:
                        result += c;
                        break;
                }
            }
            return result;
        }

        public static JSONNode Parse(string json)
        {
            Stack<JSONNode> stack = new Stack<JSONNode>();
            JSONNode resultNode = null;
            int i = 0;
            string token = "";
            string tokenName = "";
            bool quoteMode = false;
            while (i < json.Length)
            {
                switch (json[i])
                {
                    case '{':
                        if (quoteMode)
                        {
                            token += json[i];
                            break;
                        }
                        stack.Push(new JSONObject());
                        if (resultNode != null)
                        {
                            tokenName = tokenName.Trim();
                            if (resultNode is JSONArray)
                                resultNode.Add(stack.Peek());
                            else if (tokenName != "")
                                resultNode.Add(tokenName, stack.Peek());
                        }
                        tokenName = "";
                        token = "";
                        resultNode = stack.Peek();
                        break;

                    case '[':
                        if (quoteMode)
                        {
                            token += json[i];
                            break;
                        }
                        stack.Push(new JSONArray());
                        if (resultNode != null)
                        {
                            tokenName = tokenName.Trim();
                            if (resultNode is JSONArray)
                                resultNode.Add(stack.Peek());
                            else if (tokenName != "")
                                resultNode.Add(tokenName, stack.Peek());
                        }
                        tokenName = "";
                        token = "";
                        resultNode = stack.Peek();
                        break;

                    case '}':
                    case ']':
                        if (quoteMode)
                        {
                            token += json[i];
                            break;
                        }

                        if (stack.Count == 0)
                            throw new Exception("JSON Parse: Too many closing brackets");

                        stack.Pop();
                        if (token != "")
                        {
                            tokenName = tokenName.Trim();
                            if (resultNode is JSONArray)
                                resultNode.Add(token);
                            else if (tokenName != "")
                                if (resultNode != null)
                                    resultNode.Add(tokenName, token);
                        }
                        tokenName = "";
                        token = "";
                        if (stack.Count > 0)
                            resultNode = stack.Peek();
                        break;

                    case ':':
                        if (quoteMode)
                        {
                            token += json[i];
                            break;
                        }
                        tokenName = token;
                        token = "";
                        break;

                    case '"':
                        quoteMode ^= true;
                        break;

                    case ',':
                        if (quoteMode)
                        {
                            token += json[i];
                            break;
                        }
                        if (token != "")
                        {
                            if (resultNode is JSONArray)
                                resultNode.Add(token);
                            else if (tokenName != "")
                                if (resultNode != null)
                                    resultNode.Add(tokenName, token);
                        }
                        tokenName = "";
                        token = "";
                        break;

                    case '\r':
                    case '\n':
                        break;

                    case ' ':
                    case '\t':
                        if (quoteMode)
                            token += json[i];
                        break;

                    case '\\':
                        ++i;
                        if (quoteMode)
                        {
                            char c = json[i];
                            switch (c)
                            {
                                case 't':
                                    token += '\t';
                                    break;
                                case 'r':
                                    token += '\r';
                                    break;
                                case 'n':
                                    token += '\n';
                                    break;
                                case 'b':
                                    token += '\b';
                                    break;
                                case 'f':
                                    token += '\f';
                                    break;
                                case 'u':
                                    string s = json.Substring(i + 1, 4);
                                    token += (char) int.Parse(s, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    i += 4;
                                    break;
                                default:
                                    token += c;
                                    break;
                            }
                        }
                        break;

                    default:
                        token += json[i];
                        break;
                }
                ++i;
            }

            if (quoteMode)
                throw new Exception("JSON Parse: Quotation marks seems to be messed up.");

            return resultNode;
        }
    }

    public class JSONArray : JSONNode, IEnumerable
    {
        private readonly List<JSONNode> _list = new List<JSONNode>();

        public override JSONNode this[int index]
        {
            get
            {
                if (index < 0 || index >= _list.Count)
                    return new JSONLazyCreator(this);
                return _list[index];
            }
            set
            {
                if (index < 0 || index >= _list.Count)
                    _list.Add(value);
                else
                    _list[index] = value;
            }
        }

        public override JSONNode this[string key]
        {
            get { return new JSONLazyCreator(this); }
            set { _list.Add(value); }
        }

        public override int Count
        {
            get { return _list.Count; }
        }

        public override IEnumerable<JSONNode> Childs
        {
            get { return _list; }
        }

        public override void Add(JSONNode item)
        {
            _list.Add(item);
        }

        public override JSONNode Remove(int index)
        {
            if (index < 0 || index >= _list.Count)
                return null;
            JSONNode result = _list[index];
            _list.RemoveAt(index);
            return result;
        }

        public override JSONNode Remove(JSONNode node)
        {
            _list.Remove(node);
            return node;
        }

        public IEnumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public override string ToString()
        {
            string result = "[ ";
            foreach (JSONNode node in _list)
            {
                if (result.Length > 2)
                    result += ", ";
                result += node.ToString();
            }
            result += " ]";
            return result;
        }
    }

    public class JSONObject : JSONNode, IEnumerable
    {
        private readonly Dictionary<string, JSONNode> _dict = new Dictionary<string, JSONNode>();

        public override JSONNode this[string key]
        {
            get { return _dict.ContainsKey(key) ? _dict[key] : new JSONLazyCreator(this, key); }
            set
            {
                if (_dict.ContainsKey(key))
                    _dict[key] = value;
                else
                    _dict.Add(key, value);
            }
        }

        public override JSONNode this[int index]
        {
            get
            {
                if (index < 0 || index >= _dict.Count)
                    return null;
                return _dict.ElementAt(index).Value;
            }
            set
            {
                if (index < 0 || index >= _dict.Count)
                    return;
                string key = _dict.ElementAt(index).Key;
                _dict[key] = value;
            }
        }

        public override int Count
        {
            get { return _dict.Count; }
        }

        public override IEnumerable<JSONNode> Childs
        {
            get { return _dict.Select(pair => pair.Value); }
        }

        public override void Add(string key, JSONNode item)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (_dict.ContainsKey(key))
                    _dict[key] = item;
                else
                    _dict.Add(key, item);
            }
            else
            {
                _dict.Add(Guid.NewGuid().ToString(), item);
            }
        }

        public override JSONNode Remove(string key)
        {
            if (!_dict.ContainsKey(key))
                return null;
            JSONNode result = _dict[key];
            _dict.Remove(key);
            return result;
        }

        public override JSONNode Remove(int index)
        {
            if (index < 0 || index >= _dict.Count)
                return null;
            var item = _dict.ElementAt(index);
            _dict.Remove(item.Key);
            return item.Value;
        }

        public override JSONNode Remove(JSONNode node)
        {
            try
            {
                var item = _dict.First(k => k.Value == node);
                _dict.Remove(item.Key);
                return node;
            }
            catch
            {
                return null;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public override string ToString()
        {
            string result = "{";
            foreach (KeyValuePair<string, JSONNode> pair in _dict)
            {
                if (result.Length > 2)
                    result += ", ";
                result += "\"" + Escape(pair.Key) + "\":" + pair.Value.ToString();
            }
            result += "}";
            return result;
        }
    }

    public sealed class JSONData : JSONNode
    {
        private string _data;

        public override string Value
        {
            get { return _data; }
            set { _data = value; }
        }

        public JSONData(string data)
        {
            _data = data;
        }

        public JSONData(float data)
        {
            AsFloat = data;
        }

        public JSONData(double data)
        {
            AsDouble = data;
        }

        public JSONData(bool data)
        {
            AsBool = data;
        }

        public JSONData(int data)
        {
            AsInt = data;
        }

        public override string ToString()
        {
            return "\"" + Escape(_data) + "\"";
        }
    }

    internal class JSONLazyCreator : JSONNode
    {
        private JSONNode _node;
        private readonly string _key;

        public JSONLazyCreator(JSONNode node)
        {
            _node = node;
            _key = null;
        }

        public JSONLazyCreator(JSONNode node, string key)
        {
            _node = node;
            _key = key;
        }

        private void Set(JSONNode value)
        {
            if (_key == null)
                _node.Add(value);
            else
                _node.Add(_key, value);
            _node = null;
        }

        public override JSONNode this[int index]
        {
            get { return new JSONLazyCreator(this); }
            set
            {
                var result = new JSONArray {value};
                Set(result);
            }
        }

        public override JSONNode this[string key]
        {
            get { return new JSONLazyCreator(this, key); }
            set
            {
                var result = new JSONObject {{key, value}};
                Set(result);
            }
        }

        public override void Add(JSONNode item)
        {
            var result = new JSONArray {item};
            Set(result);
        }

        public override void Add(string key, JSONNode item)
        {
            var result = new JSONObject {{key, item}};
            Set(result);
        }

        public override string ToString()
        {
            return "";
        }

        #region OPERATORS

        public static bool operator ==(JSONLazyCreator a, object b)
        {
            return b == null || ReferenceEquals(a, b);
        }

        public static bool operator !=(JSONLazyCreator a, object b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj == null || ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region TYPECASTING

        public override int AsInt
        {
            get
            {
                JSONData data = new JSONData(0);
                Set(data);
                return 0;
            }
            set
            {
                JSONData data = new JSONData(value);
                Set(data);
            }
        }

        public override float AsFloat
        {
            get
            {
                JSONData data = new JSONData(0.0f);
                Set(data);
                return 0.0f;
            }
            set
            {
                JSONData data = new JSONData(value);
                Set(data);
            }
        }

        public override double AsDouble
        {
            get
            {
                JSONData data = new JSONData(0.0);
                Set(data);
                return 0.0;
            }
            set
            {
                JSONData data = new JSONData(value);
                Set(data);
            }
        }

        public override bool AsBool
        {
            get
            {
                JSONData data = new JSONData(false);
                Set(data);
                return false;
            }
            set
            {
                JSONData data = new JSONData(value);
                Set(data);
            }
        }

        public override JSONArray AsArray
        {
            get
            {
                JSONArray data = new JSONArray();
                Set(data);
                return data;
            }
        }

        public override JSONObject AsObject
        {
            get
            {
                JSONObject data = new JSONObject();
                Set(data);
                return data;
            }
        }

        #endregion
    }
}