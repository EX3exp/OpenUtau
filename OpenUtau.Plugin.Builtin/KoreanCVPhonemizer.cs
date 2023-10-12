using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using Melanchall.DryWetMidi.Core;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;
using SharpCompress;

using System.IO;
using System.Threading.Tasks;
using NWaves.Filters.Adaptive;


namespace OpenUtau.Plugin.Builtin {
     /// Phonemizer for 'KOR CV' ///
    [Phonemizer("Korean CV Phonemizer", "KO CV", "EX3", language:"KO")]

    public class KoreanCVPhonemizer : Phonemizer {

        // 1. Load Singer and Settings
        private KoreanCVIniSetting koreanCVIniSetting; // Setting object

        private bool isUsingShi = false;
        private bool isUsing_aX = false;
        private bool isUsing_i = false;
        private bool isRentan = false;
        private USinger singer;
		public override void SetSinger(USinger singer) {
            if (this.singer == singer) {
                return;
            }
            this.singer = singer;
            if (this.singer == null) {
                return;
            }

            koreanCVIniSetting = new KoreanCVIniSetting();
            koreanCVIniSetting.initialize(singer, "ko-CV.ini");

            isUsingShi = koreanCVIniSetting.isUsingShi();
            isUsing_aX = koreanCVIniSetting.isUsing_aX();
            isUsing_i = koreanCVIniSetting.isUsing_i();
            isRentan = koreanCVIniSetting.isRentan();
        }


        public abstract class IniSetting {
            protected USinger singer;
            protected IniFile iniFile = new IniFile();
            protected string iniFileName;

            public IniSetting(){ }

            
            public void initialize(USinger singer, string iniFileName){
                
                // if no [iniFileName] in Singer Directory, it makes new [iniFileName] with settings in iniSetUp(iniFile).
                this.singer = singer;
                this.iniFileName = iniFileName;
                try{
                    iniFile.Load($"{singer.Location}/{iniFileName}");
                    iniSetUp(iniFile); // you can override iniSetUp() to use.
                }
                catch{
                    iniSetUp(iniFile); // you can override iniSetUp() to use.
                }
                
            }

            protected virtual void iniSetUp(IniFile iniFile){
                /// you can override this method with your own values
                /// !! must use setOrReadThisValue(string sectionName, string keyName, bool/string/int/double value) when setting or reading values.
                /// (ex)
                /// setOrReadThisValue("sectionName", "keyName", true);
            }

            protected void setOrReadThisValue(string sectionName, string keyName, bool defaultValue){
                /// <summary>
                /// 섹션과 키 이름을 입력받고, bool 값이 존재하면 넘어가고 존재하지 않으면 defaultValue 값으로 덮어씌운다 
                /// </summary>
                iniFile[sectionName][keyName] = iniFile[sectionName][keyName].ToBool(defaultValue);

                iniFile.Save($"{singer.Location}/{iniFileName}");
            }

            protected void setOrReadThisValue(string sectionName, string keyName, string defaultValue){
                /// <summary>
                /// 섹션과 키 이름을 입력받고, string 값이 존재하면 넘어가고 존재하지 않으면 defaultValue 값으로 덮어씌운다 
                /// </summary>
                if (! iniFile[sectionName].ContainsKey(keyName)){
                    // 키가 존재하지 않으면 새로 값을 넣는다
                    iniFile[sectionName][keyName] = defaultValue;
                    iniFile.Save($"{singer.Location}/{iniFileName}");
                }
                // 키가 존재하면 그냥 스킵
            }

            protected void setOrReadThisValue(string sectionName, string keyName, int defaultValue){
                /// <summary>
                /// 섹션과 키 이름을 입력받고, int 값이 존재하면 넘어가고 존재하지 않으면 defaultValue 값으로 덮어씌운다 
                /// </summary>
                iniFile[sectionName][keyName] = iniFile[sectionName][keyName].ToInt(defaultValue);
                iniFile.Save($"{singer.Location}/{iniFileName}");
            }

            protected void setOrReadThisValue(string sectionName, string keyName, double defaultValue){
                /// <summary>
                /// 섹션과 키 이름을 입력받고, double 값이 존재하면 넘어가고 존재하지 않으면 defaultValue 값으로 덮어씌운다 
                /// </summary>
                iniFile[sectionName][keyName] = iniFile[sectionName][keyName].ToDouble(defaultValue);
                iniFile.Save($"{singer.Location}/{iniFileName}");
            }


            /// codes for ini parsing (https://github.com/Enichan/Ini/blob/master/Ini.cs)
            public struct IniValue {
    private static bool TryParseInt(string text, out int value) {
        int res;
        if (Int32.TryParse(text,
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out res)) {
            value = res;
            return true;
        }
        value = 0;
        return false;
    }

    private static bool TryParseDouble(string text, out double value) {
        double res;
        if (Double.TryParse(text,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out res)) {
            value = res;
            return true;
        }
        value = Double.NaN;
        return false;
    }

    public string Value;

    public IniValue(object value) {
        var formattable = value as IFormattable;
        if (formattable != null) {
            Value = formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
        }
        else {
            Value = value != null ? value.ToString() : null;
        }
    }

    public IniValue(string value) {
        Value = value;
    }

    public bool ToBool(bool valueIfInvalid = false) {
        bool res;
        if (TryConvertBool(out res)) {
            return res;
        }
        return valueIfInvalid;
    }

    public bool TryConvertBool(out bool result) {
        if (Value == null) {
            result = default(bool);
            return false;
        }
        var boolStr = Value.Trim().ToLowerInvariant();
        if (boolStr == "true") {
            result = true;
            return true;
        }
        else if (boolStr == "false") {
            result = false;
            return true;
        }
        result = default(bool);
        return false;
    }

    public int ToInt(int valueIfInvalid = 0) {
        int res;
        if (TryConvertInt(out res)) {
            return res;
        }
        return valueIfInvalid;
    }

    public bool TryConvertInt(out int result) {
        if (Value == null) {
            result = default(int);
            return false;
        }
        if (TryParseInt(Value.Trim(), out result)) {
            return true;
        }
        return false;
    }

    public double ToDouble(double valueIfInvalid = 0) {
        double res;
        if (TryConvertDouble(out res)) {
            return res;
        }
        return valueIfInvalid;
    }

    public bool TryConvertDouble(out double result) {
        if (Value == null) {
            result = default(double);
            return false; ;
        }
        if (TryParseDouble(Value.Trim(), out result)) {
            return true;
        }
        return false;
    }

    public string GetString() {
        return GetString(true, false);
    }

    public string GetString(bool preserveWhitespace) {
        return GetString(true, preserveWhitespace);
    }

    public string GetString(bool allowOuterQuotes, bool preserveWhitespace) {
        if (Value == null) {
            return "";
        }
        var trimmed = Value.Trim();
        if (allowOuterQuotes && trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"') {
            var inner = trimmed.Substring(1, trimmed.Length - 2);
            return preserveWhitespace ? inner : inner.Trim();
        }
        else {
            return preserveWhitespace ? Value : Value.Trim();
        }
    }

    public override string ToString() {
        return Value;
    }

    public static implicit operator IniValue(byte o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(short o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(int o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(sbyte o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(ushort o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(uint o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(float o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(double o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(bool o) {
        return new IniValue(o);
    }

    public static implicit operator IniValue(string o) {
        return new IniValue(o);
    }

    private static readonly IniValue _default = new IniValue();
    public static IniValue Default { get { return _default; } }
}

            public class IniFile : IEnumerable<KeyValuePair<string, IniSection>>, IDictionary<string, IniSection> {
    private Dictionary<string, IniSection> sections;
    public IEqualityComparer<string> StringComparer;

    public bool SaveEmptySections;

    public IniFile() 
        : this(DefaultComparer) {
    }

    public IniFile(IEqualityComparer<string> stringComparer) {
        StringComparer = stringComparer;
        sections = new Dictionary<string, IniSection>(StringComparer);
    }

    public void Save(string path, FileMode mode = FileMode.Create) {
        using (var stream = new FileStream(path, mode, FileAccess.Write)) {
            Save(stream);
        }
    }

    public void Save(Stream stream) {
        using (var writer = new StreamWriter(stream)) {
            Save(writer);
        }
    }

    public void Save(StreamWriter writer) {
        foreach (var section in sections) {
            if (section.Value.Count > 0 || SaveEmptySections) {
                writer.WriteLine(string.Format("[{0}]", section.Key.Trim()));
                foreach (var kvp in section.Value) {
                    writer.WriteLine(string.Format("{0}={1}", kvp.Key, kvp.Value));
                }
                writer.WriteLine("");
            }
        }
    }

    public void Load(string path, bool ordered = false) {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
            Load(stream, ordered);
        }
    }

    public void Load(Stream stream, bool ordered = false) {
        using (var reader = new StreamReader(stream)) {
            Load(reader, ordered);
        }
    }

    public void Load(StreamReader reader, bool ordered = false) {
        IniSection section = null;

        while (!reader.EndOfStream) {
            var line = reader.ReadLine();

            if (line != null) {
                var trimStart = line.TrimStart();

                if (trimStart.Length > 0) {
                    if (trimStart[0] == '[') {
                        var sectionEnd = trimStart.IndexOf(']');
                        if (sectionEnd > 0) {
                            var sectionName = trimStart.Substring(1, sectionEnd - 1).Trim();
                            section = new IniSection(StringComparer) { Ordered = ordered };
                            sections[sectionName] = section;
                        }
                    }
                    else if (section != null && trimStart[0] != ';') {
                        string key;
                        IniValue val;

                        if (LoadValue(line, out key, out val)) {
                            section[key] = val;
                        }
                    }
                }
            }
        }
    }

    private bool LoadValue(string line, out string key, out IniValue val) {
        var assignIndex = line.IndexOf('=');
        if (assignIndex <= 0) {
            key = null;
            val = null;
            return false;
        }

        key = line.Substring(0, assignIndex).Trim();
        var value = line.Substring(assignIndex + 1);

        val = new IniValue(value);
        return true;
    }

    public bool ContainsSection(string section) {
        return sections.ContainsKey(section);
    }

    public bool TryGetSection(string section, out IniSection result) {
        return sections.TryGetValue(section, out result);
    }

    bool IDictionary<string, IniSection>.TryGetValue(string key, out IniSection value) {
        return TryGetSection(key, out value);
    }

    public bool Remove(string section) {
        return sections.Remove(section);
    }

    public IniSection Add(string section, Dictionary<string, IniValue> values, bool ordered = false) {
        return Add(section, new IniSection(values, StringComparer) { Ordered = ordered });
    }

    public IniSection Add(string section, IniSection value) {
        if (value.Comparer != StringComparer) {
            value = new IniSection(value, StringComparer);
        }
        sections.Add(section, value);
        return value;
    }

    public IniSection Add(string section, bool ordered = false) {
        var value = new IniSection(StringComparer) { Ordered = ordered };
        sections.Add(section, value);
        return value;
    }

    void IDictionary<string, IniSection>.Add(string key, IniSection value) {
        Add(key, value);
    }

    bool IDictionary<string, IniSection>.ContainsKey(string key) {
        return ContainsSection(key);
    }

    public ICollection<string> Keys {
        get { return sections.Keys; }
    }

    public ICollection<IniSection> Values {
        get { return sections.Values; }
    }

    void ICollection<KeyValuePair<string, IniSection>>.Add(KeyValuePair<string, IniSection> item) {
        ((IDictionary<string, IniSection>)sections).Add(item);
    }

    public void Clear() {
        sections.Clear();
    }

    bool ICollection<KeyValuePair<string, IniSection>>.Contains(KeyValuePair<string, IniSection> item) {
        return ((IDictionary<string, IniSection>)sections).Contains(item);
    }

    void ICollection<KeyValuePair<string, IniSection>>.CopyTo(KeyValuePair<string, IniSection>[] array, int arrayIndex) {
        ((IDictionary<string, IniSection>)sections).CopyTo(array, arrayIndex);
    }

    public int Count {
        get { return sections.Count; }
    }

    bool ICollection<KeyValuePair<string, IniSection>>.IsReadOnly {
        get { return ((IDictionary<string, IniSection>)sections).IsReadOnly; }
    }

    bool ICollection<KeyValuePair<string, IniSection>>.Remove(KeyValuePair<string, IniSection> item) {
        return ((IDictionary<string, IniSection>)sections).Remove(item);
    }

    public IEnumerator<KeyValuePair<string, IniSection>> GetEnumerator() {
        return sections.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public IniSection this[string section] {
        get {
            IniSection s;
            if (sections.TryGetValue(section, out s)) {
                return s;
            }
            s = new IniSection(StringComparer);
            sections[section] = s;
            return s;
        }
        set {
            var v = value;
            if (v.Comparer != StringComparer) {
                v = new IniSection(v, StringComparer);
            }
            sections[section] = v;
        }
    }

    public string GetContents() {
        using (var stream = new MemoryStream()) {
            Save(stream);
            stream.Flush();
            var builder = new StringBuilder(Encoding.UTF8.GetString(stream.ToArray()));
            return builder.ToString();
        }
    }

    public static IEqualityComparer<string> DefaultComparer = new CaseInsensitiveStringComparer();

    class CaseInsensitiveStringComparer : IEqualityComparer<string> {
        public bool Equals(string x, string y) {
            return String.Compare(x, y, true) == 0;
        }

        public int GetHashCode(string obj) {
            return obj.ToLowerInvariant().GetHashCode();
        }

#if JS
        public new bool Equals(object x, object y) {
            var xs = x as string;
            var ys = y as string;
            if (xs == null || ys == null) {
                return xs == null && ys == null;
            }
            return Equals(xs, ys);
        }

        public int GetHashCode(object obj) {
            if (obj is string) {
                return GetHashCode((string)obj);
            }
            return obj.ToStringInvariant().ToLowerInvariant().GetHashCode();
        }
#endif
    }
}

            public class IniSection : IEnumerable<KeyValuePair<string, IniValue>>, IDictionary<string, IniValue> {
    private Dictionary<string, IniValue> values;

    #region Ordered
    private List<string> orderedKeys;

    public int IndexOf(string key) {
        if (!Ordered) {
            throw new InvalidOperationException("Cannot call IndexOf(string) on IniSection: section was not ordered.");
        }
        return IndexOf(key, 0, orderedKeys.Count);
    }

    public int IndexOf(string key, int index) {
        if (!Ordered) {
            throw new InvalidOperationException("Cannot call IndexOf(string, int) on IniSection: section was not ordered.");
        }
        return IndexOf(key, index, orderedKeys.Count - index);
    }

    public int IndexOf(string key, int index, int count) {
        if (!Ordered) {
            throw new InvalidOperationException("Cannot call IndexOf(string, int, int) on IniSection: section was not ordered.");
        }
        if (index < 0 || index > orderedKeys.Count) {
            throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
        }
        if (count < 0) {
            throw new IndexOutOfRangeException("Count cannot be less than zero." + Environment.NewLine + "Parameter name: count");
        }
        if (index + count > orderedKeys.Count) {
            throw new ArgumentException("Index and count were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
        }
        var end = index + count;
        for (int i = index; i < end; i++) {
            if (Comparer.Equals(orderedKeys[i], key)) {
                return i;
            }
        }
        return -1;
    }

    public int LastIndexOf(string key) {
        if (!Ordered) {
            throw new InvalidOperationException("Cannot call LastIndexOf(string) on IniSection: section was not ordered.");
        }
        return LastIndexOf(key, 0, orderedKeys.Count);
    }

    public int LastIndexOf(string key, int index) {
        if (!Ordered) {
            throw new InvalidOperationException("Cannot call LastIndexOf(string, int) on IniSection: section was not ordered.");
        }
        return LastIndexOf(key, index, orderedKeys.Count - index);
    }

    public int LastIndexOf(string key, int index, int count) {
        if (!Ordered) {
            throw new InvalidOperationException("Cannot call LastIndexOf(string, int, int) on IniSection: section was not ordered.");
        }
        if (index < 0 || index > orderedKeys.Count) {
            throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
        }
        if (count < 0) {
            throw new IndexOutOfRangeException("Count cannot be less than zero." + Environment.NewLine + "Parameter name: count");
        }
        if (index + count > orderedKeys.Count) {
            throw new ArgumentException("Index and count were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
        }
        var end = index + count;
        for (int i = end - 1; i >= index; i--) {
            if (Comparer.Equals(orderedKeys[i], key)) {
                return i;
            }
        }
        return -1;
    }

    public void Insert(int index, string key, IniValue value) {
        if (!Ordered) {
            throw new InvalidOperationException("Cannot call Insert(int, string, IniValue) on IniSection: section was not ordered.");
        }
        if (index < 0 || index > orderedKeys.Count) {
            throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
        }
        values.Add(key, value);
        orderedKeys.Insert(index, key);
    }

    public void InsertRange(int index, IEnumerable<KeyValuePair<string, IniValue>> collection) {
        if (!Ordered) {
            throw new InvalidOperationException("Cannot call InsertRange(int, IEnumerable<KeyValuePair<string, IniValue>>) on IniSection: section was not ordered.");
        }
        if (collection == null) {
            throw new ArgumentNullException("Value cannot be null." + Environment.NewLine + "Parameter name: collection");
        }
        if (index < 0 || index > orderedKeys.Count) {
            throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
        }
        foreach (var kvp in collection) {
            Insert(index, kvp.Key, kvp.Value);
            index++;
        }
    }

    public void RemoveAt(int index) {
        if (!Ordered) {
            throw new InvalidOperationException("Cannot call RemoveAt(int) on IniSection: section was not ordered.");
        }
        if (index < 0 || index > orderedKeys.Count) {
            throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
        }
        var key = orderedKeys[index];
        orderedKeys.RemoveAt(index);
        values.Remove(key);
    }

    public void RemoveRange(int index, int count) {
        if (!Ordered) {
            throw new InvalidOperationException("Cannot call RemoveRange(int, int) on IniSection: section was not ordered.");
        }
        if (index < 0 || index > orderedKeys.Count) {
            throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
        }
        if (count < 0) {
            throw new IndexOutOfRangeException("Count cannot be less than zero." + Environment.NewLine + "Parameter name: count");
        }
        if (index + count > orderedKeys.Count) {
            throw new ArgumentException("Index and count were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
        }
        for (int i = 0; i < count; i++) {
            RemoveAt(index);
        }
    }

    public void Reverse() {
        if (!Ordered) {
            throw new InvalidOperationException("Cannot call Reverse() on IniSection: section was not ordered.");
        }
        orderedKeys.Reverse();
    }

    public void Reverse(int index, int count) {
        if (!Ordered) {
            throw new InvalidOperationException("Cannot call Reverse(int, int) on IniSection: section was not ordered.");
        }
        if (index < 0 || index > orderedKeys.Count) {
            throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
        }
        if (count < 0) {
            throw new IndexOutOfRangeException("Count cannot be less than zero." + Environment.NewLine + "Parameter name: count");
        }
        if (index + count > orderedKeys.Count) {
            throw new ArgumentException("Index and count were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
        }
        orderedKeys.Reverse(index, count);
    }

    public ICollection<IniValue> GetOrderedValues() {
        if (!Ordered) {
            throw new InvalidOperationException("Cannot call GetOrderedValues() on IniSection: section was not ordered.");
        }
        var list = new List<IniValue>();
        for (int i = 0; i < orderedKeys.Count; i++) {
            list.Add(values[orderedKeys[i]]);
		}
        return list;
    }

    public IniValue this[int index] {
        get {
            if (!Ordered) {
                throw new InvalidOperationException("Cannot index IniSection using integer key: section was not ordered.");
            }
            if (index < 0 || index >= orderedKeys.Count) {
                throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
            }
            return values[orderedKeys[index]];
        }
        set {
            if (!Ordered) {
                throw new InvalidOperationException("Cannot index IniSection using integer key: section was not ordered.");
            }
            if (index < 0 || index >= orderedKeys.Count) {
                throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
            }
            var key = orderedKeys[index];
            values[key] = value;
        }
    }

    public bool Ordered {
        get {
            return orderedKeys != null;
        }
        set {
            if (Ordered != value) {
                orderedKeys = value ? new List<string>(values.Keys) : null;
            }
        }
    }
    #endregion

    public IniSection()
        : this(IniFile.DefaultComparer) {
    }

    public IniSection(IEqualityComparer<string> stringComparer) {
        this.values = new Dictionary<string, IniValue>(stringComparer);
    }

    public IniSection(Dictionary<string, IniValue> values)
        : this(values, IniFile.DefaultComparer) {
    }

    public IniSection(Dictionary<string, IniValue> values, IEqualityComparer<string> stringComparer) {
        this.values = new Dictionary<string, IniValue>(values, stringComparer);
    }

    public IniSection(IniSection values)
        : this(values, IniFile.DefaultComparer) {
    }

    public IniSection(IniSection values, IEqualityComparer<string> stringComparer) {
        this.values = new Dictionary<string, IniValue>(values.values, stringComparer);
    }

    public void Add(string key, IniValue value) {
        values.Add(key, value);
        if (Ordered) {
            orderedKeys.Add(key);
        }
    }

    public bool ContainsKey(string key) {
        return values.ContainsKey(key);
    }

    /// <summary>
    /// Returns this IniSection's collection of keys. If the IniSection is ordered, the keys will be returned in order.
    /// </summary>
    public ICollection<string> Keys {
        get { return Ordered ? (ICollection<string>)orderedKeys : values.Keys; }
    }

    public bool Remove(string key) {
        var ret = values.Remove(key);
        if (Ordered && ret) {
            for (int i = 0; i < orderedKeys.Count; i++) {
                if (Comparer.Equals(orderedKeys[i], key)) {
                    orderedKeys.RemoveAt(i);
                    break;
                }
            }
        }
        return ret;
    }

    public bool TryGetValue(string key, out IniValue value) {
        return values.TryGetValue(key, out value);
    }

    /// <summary>
    /// Returns the values in this IniSection. These values are always out of order. To get ordered values from an IniSection call GetOrderedValues instead.
    /// </summary>
    public ICollection<IniValue> Values {
        get {
            return values.Values;
        }
    }

    void ICollection<KeyValuePair<string, IniValue>>.Add(KeyValuePair<string, IniValue> item) {
        ((IDictionary<string, IniValue>)values).Add(item);
        if (Ordered) {
            orderedKeys.Add(item.Key);
        }
    }

    public void Clear() {
        values.Clear();
        if (Ordered) {
            orderedKeys.Clear();
        }
    }

    bool ICollection<KeyValuePair<string, IniValue>>.Contains(KeyValuePair<string, IniValue> item) {
        return ((IDictionary<string, IniValue>)values).Contains(item);
    }

    void ICollection<KeyValuePair<string, IniValue>>.CopyTo(KeyValuePair<string, IniValue>[] array, int arrayIndex) {
        ((IDictionary<string, IniValue>)values).CopyTo(array, arrayIndex);
    }

    public int Count {
        get { return values.Count; }
    }

    bool ICollection<KeyValuePair<string, IniValue>>.IsReadOnly {
        get { return ((IDictionary<string, IniValue>)values).IsReadOnly; }
    }

    bool ICollection<KeyValuePair<string, IniValue>>.Remove(KeyValuePair<string, IniValue> item) {
        var ret = ((IDictionary<string, IniValue>)values).Remove(item);
        if (Ordered && ret) {
            for (int i = 0; i < orderedKeys.Count; i++) {
                if (Comparer.Equals(orderedKeys[i], item.Key)) {
                    orderedKeys.RemoveAt(i);
                    break;
                }
            }
        }
        return ret;
    }

    public IEnumerator<KeyValuePair<string, IniValue>> GetEnumerator() {
        if (Ordered) {
            return GetOrderedEnumerator();
        }
        else {
            return values.GetEnumerator();
        }
    }

    private IEnumerator<KeyValuePair<string, IniValue>> GetOrderedEnumerator() {
        for (int i = 0; i < orderedKeys.Count; i++) {
            yield return new KeyValuePair<string, IniValue>(orderedKeys[i], values[orderedKeys[i]]);
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public IEqualityComparer<string> Comparer { get { return values.Comparer; } }

    public IniValue this[string name] {
        get {
            IniValue val;
            if (values.TryGetValue(name, out val)) {
                return val;
            }
            return IniValue.Default;
        }
        set {
            if (Ordered && !orderedKeys.Contains(name, Comparer)) {
                orderedKeys.Add(name);
            }
            values[name] = value;
        }
    }

    public static implicit operator IniSection(Dictionary<string, IniValue> dict) {
        return new IniSection(dict);
    }

    public static explicit operator Dictionary<string, IniValue>(IniSection section) {
        return section.values;
    }
}
            

        }
        private class Hanguel : KoreanCBNNPhonemizer.Hanguel{}
        private class KoreanCVIniSetting : IniSetting {
            protected override void iniSetUp(IniFile iniFile) {
                // ko-CV.ini
                setOrReadThisValue("CV", "Use rentan", false); // 연단음 사용 유무 - 기본값 false
                setOrReadThisValue("CV", "Use 'shi' for '시'(otherwise 'si')", false); // 시를 [shi]로 표기할 지 유무 - 기본값 false
                setOrReadThisValue("CV", "Use 'i' for '의'(otherwise 'eui')", false); // 의를 [i]로 표기할 지 유무 - 기본값 false
                setOrReadThisValue("BATCHIM", "Use 'aX' instead of 'a X'", false); // 받침 표기를 a n 처럼 할 지 an 처럼 할지 유무 - 기본값 false(=a n 사용)
            }

            public bool isRentan(){
                bool isRentan = iniFile["CV"]["Use rentan"].ToBool();
                return isRentan;
            }

            public bool isUsingShi(){
                bool isUsingShi = iniFile["CV"]["Use 'shi' for '시'(otherwise 'si')"].ToBool();
                return isUsingShi;
            }

            public bool isUsing_aX(){
                bool isUsing_aX = iniFile["BATCHIM"]["Use 'aX' instead of 'a X'"].ToBool();
                return isUsing_aX;
            }

            public bool isUsing_i(){
                bool isUsing_i = iniFile["CV"]["Use 'i' for '의'(otherwise 'eui')"].ToBool();
                return isUsing_i;
            }

        }
        private class CV{
            static readonly string[] PHONEME_TYPES = new string[]{
                "none", "voiced", "aspirate", "fortis", "fricative", "liquid", "nasal"
                }; // 음소없음, 유성음, 파열음&파찰음, 경음, 마찰음, 유음, 비음 
            static readonly Dictionary<string, string[]> FIRST_CONSONANTS = new Dictionary<string, string[]>(){
                {"ㄱ", new string[2]{"g", "voiced"}}, 
                {"ㄲ", new string[2]{"gg", "fortis"}}, 
                {"ㄴ", new string[2]{"n", "nasal"}}, 
                {"ㄷ", new string[2]{"d", "voiced"}}, 
                {"ㄸ", new string[2]{"dd", "fortis"}}, 
                {"ㄹ", new string[2]{"r", "liquid"}}, 
                {"ㅁ", new string[2]{"m", "nasal"}}, 
                {"ㅂ", new string[2]{"b", "voiced"}}, 
                {"ㅃ", new string[2]{"bb", "fortis"}}, 
                {"ㅅ", new string[2]{"s", "fricative"}}, 
                {"ㅆ", new string[2]{"ss", "fricative"}}, 
                {"ㅇ", new string[2]{"", "none"}}, 
                {"ㅈ", new string[2]{"j", "voiced"}}, 
                {"ㅉ", new string[2]{"jj", "fortis"}}, 
                {"ㅊ", new string[2]{"ch", "aspirate"}}, 
                {"ㅋ", new string[2]{"k", "aspirate"}}, 
                {"ㅌ", new string[2]{"t", "aspirate"}}, 
                {"ㅍ", new string[2]{"p", "aspirate"}}, 
                {"ㅎ", new string[2]{"h", "fricative"}},
                {" ", new string[2]{"", "null"}},
                {"null", new string[2]{"", ""}} // 뒤 글자가 없을 때를 대비
                }; 


            static readonly Dictionary<string, string[]> MIDDLE_VOWELS = new Dictionary<string, string[]>(){
                {"ㅏ", new string[3]{"a", "", "a"}}, 
                {"ㅐ", new string[3]{"e", "", "e"}}, 
                {"ㅑ", new string[3]{"ya", "y", "a"}}, 
                {"ㅒ", new string[3]{"ye", "y", "e"}}, 
                {"ㅓ", new string[3]{"eo", "", "eo"}}, 
                {"ㅔ", new string[3]{"e", "", "e"}}, 
                {"ㅕ", new string[3]{"yeo", "y", "eo"}}, 
                {"ㅖ", new string[3]{"ye", "y", "e"}}, 
                {"ㅗ", new string[3]{"o", "", "o"}}, 
                {"ㅘ", new string[3]{"wa", "w", "a"}}, 
                {"ㅙ", new string[3]{"we", "w", "e"}}, 
                {"ㅛ", new string[3]{"yo", "y", "o"}}, 
                {"ㅜ", new string[3]{"u", "", "u"}}, 
                {"ㅝ", new string[3]{"weo", "w", "eo"}}, 
                {"ㅞ", new string[3]{"we", "w", "e"}}, 
                {"ㅟ", new string[3]{"wi", "w", "i"}}, 
                {"ㅠ", new string[3]{"yu", "y", "u"}}, 
                {"ㅡ", new string[3]{"eu", "", "eu"}}, 
                {"ㅢ", new string[3]{"i", "", "i"}}, // ㅢ는 ㅣ로 발음
                {"ㅣ", new string[3]{"i", "", "i"}},
                {" ", new string[3]{"", "", ""}},
                {"null", new string[3]{"", "", ""}} // 뒤 글자가 없을 때를 대비
                }; 

            static readonly Dictionary<string, string[]> LAST_CONSONANTS = new Dictionary<string, string[]>(){
                 //ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ
                {"ㄱ", new string[2]{"k", ""}}, 
                {"ㄲ", new string[2]{"k", ""}}, 
                {"ㄳ", new string[2]{"k", ""}}, 
                {"ㄴ", new string[2]{"n", "2"}}, 
                {"ㄵ", new string[2]{"n", "2"}}, 
                {"ㄶ", new string[2]{"n", "2"}}, 
                {"ㄷ", new string[2]{"t", "1"}}, 
                {"ㄹ", new string[2]{"l", "4"}}, 
                {"ㄺ", new string[2]{"k", ""}}, 
                {"ㄻ", new string[2]{"m", "1"}}, 
                {"ㄼ", new string[2]{"l", "4"}}, 
                {"ㄽ", new string[2]{"l", "4"}}, 
                {"ㄾ", new string[2]{"l", "4"}}, 
                {"ㄿ", new string[2]{"p", "1"}}, 
                {"ㅀ", new string[2]{"l", "4"}}, 
                {"ㅁ", new string[2]{"m", "1"}}, 
                {"ㅂ", new string[2]{"p", "1"}}, 
                {"ㅄ", new string[2]{"p", "1"}}, 
                {"ㅅ", new string[2]{"t", "1"}},
                {"ㅆ", new string[2]{"t", "1"}}, 
                {"ㅇ", new string[2]{"ng", "3"}}, 
                {"ㅈ", new string[2]{"t", "1"}}, 
                {"ㅊ", new string[2]{"t", "1"}}, 
                {"ㅋ", new string[2]{"k", ""}}, 
                {"ㅌ", new string[2]{"t", "1"}}, 
                {"ㅍ", new string[2]{"p", "1"}}, 
                {"ㅎ", new string[2]{"t", "1"}},
                {" ", new string[2]{"", ""}},
                {"null", new string[2]{"", ""}} // 뒤 글자가 없을 때를 대비
                };
            

            private Hanguel hanguel = new Hanguel();

            public CV(){}
            public Hashtable convertForCV(Hashtable separated, bool[] setting){
                // Hangeul.separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CV식으로 변경
                Hashtable separatedConvertedForCV;

                separatedConvertedForCV = new Hashtable(){
                    [0] = FIRST_CONSONANTS[(string)separated[0]][0],
                    [1] = MIDDLE_VOWELS[(string)separated[1]][1],
                    [2] = MIDDLE_VOWELS[(string)separated[1]][2],
                    [3] = LAST_CONSONANTS[(string)separated[2]][0],

                    [4] = FIRST_CONSONANTS[(string)separated[3]][0],
                    [5] = MIDDLE_VOWELS[(string)separated[4]][1],
                    [6] = MIDDLE_VOWELS[(string)separated[4]][2],
                    [7] = LAST_CONSONANTS[(string)separated[5]][0],

                    [8] = FIRST_CONSONANTS[(string)separated[6]][0],
                    [9] = MIDDLE_VOWELS[(string)separated[7]][1],
                    [10] = MIDDLE_VOWELS[(string)separated[7]][2],
                    [11] = LAST_CONSONANTS[(string)separated[8]][0]
                    };

                if ((setting[0]) && (separatedConvertedForCV[4].Equals("s")) && (separatedConvertedForCV[6].Equals("i"))){ 
                    // [isUsingShi], isUsing_aX, isUsing_i, isRentan
                    separatedConvertedForCV[4] = "sh"; // si to shi
                }
                else if ((! setting[2]) && (separated[4].Equals("ㅢ"))){
                    // isUsingShi, isUsing_aX, [isUsing_i], isRentan
                    separatedConvertedForCV[5] = "eu"; // to eui
                }

                return separatedConvertedForCV;
            }

            public Hashtable convertForCV(Note? prevNeighbour, Note note, Note? nextNeighbour, bool[] setting){
                // Hangeul.separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CV식으로 변경
                // 이 함수만 불러서 모든 것을 함 (1) [냥]냥
                return convertForCV(hanguel.variate(prevNeighbour, note, nextNeighbour), setting);
                
            }

        }
        

        private string? findInOto(string phoneme, Note note, bool nullIfNotFound=false){
            // 음소와 노트를 입력받고, 다음계 및 보이스컬러 에일리어스를 적용한다. 
            // nullIfNotFound가 true이면 음소가 찾아지지 않을 때 음소가 아닌 null을 리턴한다.
            // nullIfNotFound가 false면 음소가 찾아지지 않을 때 그대로 음소를 반환
            string phonemeToReturn;
            string color = string.Empty;
            int toneShift = 0;
            int? alt = null;
            if (phoneme.Equals("")) {
                return phoneme;
            }
            
            if (singer.TryGetMappedOto(phoneme + alt, note.tone + toneShift, color, out var otoAlt)) {
                phonemeToReturn = otoAlt.Alias;
            } else if (singer.TryGetMappedOto(phoneme, note.tone + toneShift, color, out var oto)) {
                phonemeToReturn = oto.Alias;
            } 
            else if (singer.TryGetMappedOto(phoneme, note.tone, color, out oto)){
                phonemeToReturn = oto.Alias;
            }
            else if (nullIfNotFound) {
                phonemeToReturn = null;
            }
            else{
                phonemeToReturn = phoneme;
            }

            return phonemeToReturn;
        }


        // 2. Return Phonemes
        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours){
            Hashtable cvPhonemes;

            Note note = notes[0];
            string lyric = note.lyric;
            string phoneticHint = note.phoneticHint;

            Note? prevNote = prevNeighbour; // null or Note
            Note thisNote = note;
            Note? nextNote = nextNeighbour; // null or Note

            int totalDuration = notes.Sum(n => n.duration); 
            int vcLength = 120; // TODO
            int vcLengthShort = 30;

            Hanguel hanguel = new Hanguel();
            CV cv = new CV();
            ///
            if ( phoneticHint != null){
                // if there are phonetic hint
                // 발음 힌트가 있음 
                // 냥[nya2, ang]
                string[] phoneticHints = phoneticHint.Split(','); // phonemes are seperated by ','.
                int phoneticHintsLength = phoneticHints.Length; 
                
                

                Phoneme[] phonemes = new Phoneme[phoneticHintsLength];

                Dictionary<string, string> VVdictionary = new Dictionary<string, string>(){};

                string[] VVsource = new string[] {"a", "i", "u", "e", "o", "eo", "eu"};


                for (int i = 0; i < 7; i++){
                    // VV 딕셔너리를 채운다
                    // 나중에 발음기호에 ["a a"]를 입력하고 만일 음원에게 "a a"가 없을 경우, 자동으로 VVDictionary에서 "a a"에 해당하는 값인 "a"를 호출해 사용
                    // (반대도 똑같이 적용)

                    // VVDictionary 예시: {"a a", "a"} ...
                    for (int j = 6; j >= 0; j--){
                        VVdictionary[$"{VVsource[i]} {VVsource[j]}"] = $"{VVsource[j]}"; // CV/CVC >> CBNN 호환용
                        VVdictionary[$"{VVsource[j]}"] = $"{VVsource[i]} {VVsource[j]}"; // CBNN >> CV/CVC 호환용
                    }
                    
                }

                for (int i = 0; i < phoneticHintsLength; i++){
                    string? alias = findInOto(phoneticHints[i].Trim(), note, true); // alias if exists, otherwise null
                    
                    if (alias != null){
                        // 발음기호에 입력된 phoneme이 음원에 존재함

                        if (i == 0){
                        // first syllable
                        phonemes[i] = new Phoneme {phoneme = alias};
                    }
                    else if ((i == phoneticHintsLength - 1) && ((phoneticHints[i].Trim().EndsWith('-')) || phoneticHints[i].Trim().EndsWith('R'))){
                        // 마지막 음소이고 끝음소(ex: a -, a R)일 경우, VCLengthShort에 맞춰 음소를 배치
                        phonemes[i] = new Phoneme {
                            phoneme = alias,
                            position = totalDuration - Math.Min(vcLengthShort, totalDuration / 8)
                            // 8등분한 길이로 끝에 숨소리 음소 배치, n등분했을 때의 음소 길이가 이보다 작다면 n등분했을 때의 길이로 간다
                        };
                    }
                    else if (phoneticHintsLength == 2){
                        // 입력되는 발음힌트가 2개일 경우, 2등분되어 음소가 배치된다.
                        // 이 경우 부자연스러우므로 3등분해서 음소 배치하게 조정
                        phonemes[i] = new Phoneme {
                            phoneme = alias,
                            position = totalDuration - totalDuration / 3
                            // 3등분해서 음소가 배치됨
                        };
                    }
                    else {
                        phonemes[i] = new Phoneme {
                            phoneme = alias,
                            position = totalDuration - ((totalDuration / phoneticHintsLength) * (phoneticHintsLength - i))
                            // 균등하게 n등분해서 음소가 배치됨
                        };
                    }
                    }
                    
                    else if (VVdictionary.ContainsKey(phoneticHints[i].Trim())){
                        // 입력 실패한 음소가 VV 혹은 V일 때
                        if (phoneticHintsLength == 2){
                        // 입력되는 발음힌트가 2개일 경우, 2등분되어 음소가 배치된다.
                        // 이 경우 부자연스러우므로 3등분해서 음소 배치하게 조정
                        phonemes[i] = new Phoneme {
                            phoneme = findInOto(VVdictionary[phoneticHints[i].Trim()], note),
                            position = totalDuration - totalDuration / 3
                            // 3등분해서 음소가 배치됨
                        };
                    }
                    else{
                        phonemes[i] = new Phoneme {
                            phoneme = findInOto(VVdictionary[phoneticHints[i].Trim()], note),
                            position = totalDuration - ((totalDuration / phoneticHintsLength) * (phoneticHintsLength - i))
                            // 균등하게 n등분해서 음소가 배치됨
                        };
                    }
                    }
                    else{
                        // 그냥 음원에 음소가 없음
                        phonemes[i] = new Phoneme {
                            phoneme = phoneticHints[i].Trim(),
                            position = totalDuration - ((totalDuration / phoneticHintsLength) * (phoneticHintsLength - i))
                            // 균등하게 n등분해서 음소가 배치됨
                        };
                    }

                }

                return new Result(){
                        phonemes = phonemes
                        };
            }
            else if (hanguel.isHangeul(lyric)){
                try{
                    cvPhonemes = cv.convertForCV(prevNote, thisNote, nextNote, new bool[]{isUsingShi, isUsing_aX, isUsing_i, isRentan}); // [isUsingShi], isUsing_aX, isUsing_i, isRentan
                // 음운변동이 진행됨 => 위에서 반환된 음소로 전부 때울 예정
                }
                catch{
                    return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = lyric},
                            }
                        };
                }
                

                // ex 냥냐 (nya3 ang nya)
                string thisFirstConsonant = (string)cvPhonemes[4]; // n
                string thisVowelHead = (string)cvPhonemes[5]; // y
                string thisVowelTail = (string)cvPhonemes[6]; // a
                string thisLastConsonant = (string)cvPhonemes[7]; // ng

                string nextFirstConsonant; // VC음소 만들 때 쓰는 다음 노트의 자음 음소 / CVC 음소와는 관계 없음 // ny
                string nextVowelHead = (string)cvPhonemes[9]; // 다음 노트 모음의 머리 음소 / y

                string prevVowelTail = (string)cvPhonemes[2]; // VV음소 만들 때 쓰는 이전 노트의 모음 음소 / CV, CVC 음소와는 관계 없음 // a
                string prevLastConsonant = (string)cvPhonemes[3]; // VV음소 만들 때 쓰는 이전 노트의 받침 음소

                string VV = $"{thisVowelTail}"; // a
                string CV = $"{thisFirstConsonant}{thisVowelHead}{thisVowelTail}"; // nya4
                
                string cVC;
                string frontCV; // 연단음일 때엔 -가 붙은 형식으로 저장되는 변수 
                string? endSoundVowel = findInOto($"{thisVowelTail} -", note, true);
                string? endSoundLastConsonant = findInOto($"{thisLastConsonant} -", note, true);

                int cVCLength; // 받침 종류에 따라 길이가 달라짐 / 이웃이 있을 때에만 사용

                if (thisLastConsonant.Equals("l")){
                    // ㄹ받침
                    cVCLength = totalDuration / 2;
                }
                else if (thisLastConsonant.Equals("n")){
                    // ㄴ받침
                    cVCLength = 170;
                }
                else if (thisLastConsonant.Equals("ng")){
                    // ㅇ받침
                    cVCLength = 230;
                }
                else if (thisLastConsonant.Equals("m")){
                    // ㅁ받침
                    cVCLength = 280;
                }
                else if (thisLastConsonant.Equals("k")){
                    // ㄱ받침
                    cVCLength = totalDuration / 2;
                }
                else if (thisLastConsonant.Equals("t")){
                    // ㄷ받침
                    cVCLength = totalDuration / 2;
                }
                else if (thisLastConsonant.Equals("p")){
                    cVCLength = totalDuration / 2;
                }
                else{
                    // 나머지
                    cVCLength = totalDuration / 3;
                }

                if (thisVowelTail.Equals("u")){
                    cVCLength += 50; // 모음이 u일때엔 cVC의 발음 길이가 더 길어짐
                    vcLength += 50;
                }

                if (isUsing_aX) {
                    // 받침 음소를 aX 형식으로 사용
                    cVC = $"{thisVowelTail}{thisLastConsonant}"; // ang 
                }
                else{
                    // 받침 음소를 a X 형식으로 사용
                    cVC = $"{thisVowelTail} {thisLastConsonant}"; // a ng 
                }
                

                if (! isUsing_i) {
                    // ㅢ를 ㅣ로 대체해서 발음하지 않을 때
                    if (singer.TryGetMappedOto($"{CV}", thisNote.tone, out UOto oto)) {
                        // (consonant)eui 있는지 체크
                        CV = $"{CV}";
                    }
                    else{
                        // (consonant)eui 없으면 i 사용
                        CV = $"{thisFirstConsonant}{thisVowelTail}";
                    }
                    
                }

                if (isRentan) {
                    // 연단음 / 어두 음소(-) 사용 
                    if (findInOto($"- {CV}", note, true) == null) {
                        if (findInOto($"-{CV}", note, true) == null){
                            frontCV = findInOto($"-{CV}", note, true);
                            CV = findInOto($"{CV}", note);
                        }
                        frontCV = findInOto($"-{CV}", note, true);
                        CV = findInOto($"{CV}", note);
                    }
                    else{
                        CV = findInOto($"{CV}", note);
                        frontCV = CV;
                    }
                    
                }

                else{
                    // 연단음 아님 / 어두 음소(-) 미사용
                    CV = findInOto($"{CV}", note);
                    frontCV = CV;
                    
                }

                
                if ((nextVowelHead.Equals("w")) && (thisVowelTail.Equals("eu"))) {
                    nextFirstConsonant = $"{(string)cvPhonemes[8]}"; // VC에 썼을 때 eu bw 대신 eu b를 만들기 위함
                }
                else if ((nextVowelHead.Equals("y") && (thisVowelTail.Equals("i")))){
                    nextFirstConsonant = $"{(string)cvPhonemes[8]}"; // VC에 썼을 때 i by 대신 i b를 만들기 위함
                }
                else {
                    nextFirstConsonant = $"{(string)cvPhonemes[8]}{(string)cvPhonemes[9]}"; // 나머지... ex) ny
                }

                string VC = $"{thisVowelTail} {nextFirstConsonant}"; // 다음에 이어질 VC, CVC에게는 해당 없음

                
                VC = findInOto(VC, note);
                VV = findInOto(VV, note);
                cVC = findInOto(cVC, note);
                if (endSoundVowel == null){
                    endSoundVowel = "";
                }
                if (endSoundLastConsonant == null) {
                    endSoundLastConsonant = "";
                }
                
                if (frontCV == null){
                    frontCV = CV;
                }
                // return phonemes
                if ((prevNeighbour == null) && (nextNeighbour == null)){
                    // 이웃이 없음 / 냥
                    
                    if (thisLastConsonant.Equals("")){ // 이웃 없고 받침 없음 / 냐
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{endSoundVowel}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                        };
                    }
                    else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m"))){
                        // 이웃 없고 받침 있음 - ㄴㄹㅇㅁ / 냥
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                    }
                    else{
                        // 이웃 없고 받침 있음 - 나머지 / 냑
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                    }
                }

                else if ((prevNeighbour != null) && (nextNeighbour == null)){
                        // 앞에 이웃 있고 뒤에 이웃 없음 / 냥[냥]
                        if (thisLastConsonant.Equals("")){ // 뒤이웃만 없고 받침 없음 / 냐[냐]
                        if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[아]
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{endSoundVowel}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)},
                            }
                        };
                        }
                        else if ((! prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (! thisVowelTail.Equals(""))){
                            // 앞에 받침이 온 모음 / 냥[아]
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{endSoundVowel}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)}
                            }
                        };
                        }
                        else{
                            // 모음아님 / 냐[냐]
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{endSoundVowel}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)},
                            }
                        };
                        }
                        
                    }
                    else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m"))){
                        // 뒤이웃만 없고 받침 있음 - ㄴㄹㅇㅁ  / 냐[냥]

                        if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[앙]
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                        }
                        else if ((! prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (! thisVowelTail.Equals(""))){
                            // 앞에 받침이 온 모음 / 냥[앙]
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                        }
                        else{
                            // 모음 아님 / 냥[냥]
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                        }
                        
                    }
                    else{
                        // 뒤이웃만 없고 받침 있음 - 나머지 / 냐[냑]
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                        };
                    }

                        
                    }
                else if ((prevNeighbour == null) && (nextNeighbour != null)){
                    if (hanguel.isHangeul(nextNeighbour?.lyric)){
                        // 뒤 글자가 한글임
                        // 앞에 이웃 없고 뒤에 있음

                    if (thisLastConsonant.Equals("")){ // 앞이웃만 없고 받침 없음 / [냐]냥
                        if (nextFirstConsonant.Equals("")){
                            // 뒤에 VV 와야해서 VC 오면 안됨
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{frontCV}"},
                            }
                        };
                        }
                        else{
                            if ((nextFirstConsonant.Equals("k")) || (nextFirstConsonant.Equals("t")) || (nextFirstConsonant.Equals("p")) || (nextFirstConsonant.Equals("ch")) || (nextFirstConsonant.Equals("gg")) || (nextFirstConsonant.Equals("dd")) || (nextFirstConsonant.Equals("bb")) || (nextFirstConsonant.Equals("ss")) || (nextFirstConsonant.Equals("jj"))){
                                // 뒤 음소가 파열음 혹은 된소리일 때엔 VC로 공백을 준다 
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                            };
                            }
                            else {
                                // 뒤 음소가 파열음이나 된소리가 아니면 그냥 이어줌
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{frontCV}"},
                            }
                            };
                            }
                        }
                        
                    }
                    else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m")) || (nextFirstConsonant.Equals("ss"))){
                        // 앞이웃만 없고 받침 있음 - ㄴㄹㅇㅁ + 뒤에 오는 음소가 ㅆ임 / [냥]냐
                        if ((nextFirstConsonant.Equals("n")) || (nextFirstConsonant.Equals("r")) || (nextFirstConsonant.Equals("ng")) || (nextFirstConsonant.Equals("m")) || (nextFirstConsonant.Equals("g")) || (nextFirstConsonant.Equals("d")) || (nextFirstConsonant.Equals("b")) || (nextFirstConsonant.Equals("gy")) || (nextFirstConsonant.Equals("dy")) || (nextFirstConsonant.Equals("by")) || (nextFirstConsonant.Equals("gw")) || (nextFirstConsonant.Equals("dw")) || (nextFirstConsonant.Equals("bw")) || (nextFirstConsonant.Equals("s")) || (nextFirstConsonant.Equals("sy")) || (nextFirstConsonant.Equals("sw")) || (nextFirstConsonant.Equals("j")) || (nextFirstConsonant.Equals("jy")) || (nextFirstConsonant.Equals("jw"))){
                            return new Result(){
                        phonemes = new Phoneme[] {
                            // 다음 음소가 ㄴㅇㄹㅇ ㄱㄷㅂ 임 
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }// -음소 없이 이어줌
                        };
                        }
                        else{
                            // 다음 음소가 나머지임
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)},
                            }// -음소 있이 이어줌
                        };
                        }
                        
                    }
                    else{
                        // 앞이웃만 없고 받침 있음 - 나머지 / [꺅]꺄
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                    }
                    }
                    else if (nextNeighbour?.lyric == "-"){
                        if (thisLastConsonant.Equals("")){
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{frontCV}"},
                            }
                        };
                        }
                        else{
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{frontCV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                    }
                    }
                    else{
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            }
                        };
                    }
                }
                else if ((prevNeighbour != null) && (nextNeighbour != null)){
                    // 둘다 이웃 있음
                    if (hanguel.isHangeul(nextNeighbour?.lyric)){
                        // 뒤의 이웃이 한국어임

                    if (thisLastConsonant.Equals("")){ // 둘다 이웃 있고 받침 없음 / 냥[냐]냥
                        if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals("")) && (nextFirstConsonant.Equals(""))){
                            // 앞에 받침 없는 모음 / 뒤에 모음 옴 / 냐[아]아
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            }
                        };
                        }
                        else if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 뒤에 자음 옴 / 냐[아]냐
                            if ((nextFirstConsonant.Equals("k")) || (nextFirstConsonant.Equals("t")) || (nextFirstConsonant.Equals("p")) || (nextFirstConsonant.Equals("ch")) || (nextFirstConsonant.Equals("gg")) || (nextFirstConsonant.Equals("dd")) || (nextFirstConsonant.Equals("bb")) || (nextFirstConsonant.Equals("ss")) || (nextFirstConsonant.Equals("jj"))){
                                // 뒤 음소가 파열음 혹은 된소리일 때엔 VC로 공백을 준다 
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"",
                            position = totalDuration - totalDuration / 2},
                            }
                            };
                            }
                            else {
                                // 뒤 음소가 파열음이나 된소리가 아니면 그냥 이어줌
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            }
                            };
                            }

                        }
                        else {
                            // 앞에 받침 있는 모음 + 모음 아님 / 냐[냐]냐  냥[아]냐
                            if (nextFirstConsonant.Equals("")){
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            }
                        };
                            }
                            else if ((nextFirstConsonant.Equals("k")) || (nextFirstConsonant.Equals("t")) || (nextFirstConsonant.Equals("p")) || (nextFirstConsonant.Equals("ch")) || (nextFirstConsonant.Equals("gg")) || (nextFirstConsonant.Equals("dd")) || (nextFirstConsonant.Equals("bb")) || (nextFirstConsonant.Equals("ss")) || (nextFirstConsonant.Equals("jj"))){
                                // 뒤 음소가 파열음 혹은 된소리일 때엔 VC로 공백을 준다 
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = "",
                            position = totalDuration - totalDuration / 2},
                            }
                            };
                            }
                            else{
                                // 뒤 음소가 파열음이나 된소리가 아니면 그냥 이어줌
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            }
                        };
                            }
                            
                        }
                        
                    }
                    else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m")) || (nextFirstConsonant.Equals("ss"))){
                        // 둘다 이웃 있고 받침 있음 - ㄴㄹㅇㅁ + 뒤에 오는 음소가 ㅆ임 / 냐[냥]냐
                        if ((nextFirstConsonant.Equals("n")) || (nextFirstConsonant.Equals("r")) || (nextFirstConsonant.Equals("")) || (nextFirstConsonant.Equals("m")) || (nextFirstConsonant.Equals("g")) || (nextFirstConsonant.Equals("d")) || (nextFirstConsonant.Equals("b")) || (nextFirstConsonant.Equals("gy")) || (nextFirstConsonant.Equals("dy")) || (nextFirstConsonant.Equals("by")) || (nextFirstConsonant.Equals("gw")) || (nextFirstConsonant.Equals("dw")) || (nextFirstConsonant.Equals("bw")) || (nextFirstConsonant.Equals("s")) || (nextFirstConsonant.Equals("sy")) || (nextFirstConsonant.Equals("sw")) || (nextFirstConsonant.Equals("j")) || (nextFirstConsonant.Equals("jy")) || (nextFirstConsonant.Equals("jw"))){
                            // 다음 음소가 ㄴㅇㄹㅇ ㄱㄷㅂㅅㅈ 임
                            if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[앙]냐
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                        }
                        else{
                            // 앞에 받침이 있는 모음 + 모음 아님 / 냥[앙]냐 냥[냥]냥
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength),}
                            }// -음소 없이 이어줌
                        };
                        }
                            
                        }
                        else{
                            // 다음 음소가 ㄴㅇㄹㅁ 제외 나머지임
                            if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[앙]꺅
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)}
                            }
                        };
                        }
                            else{
                                // 앞에 받침 있는 모음 + 모음 아님 / 냥[앙]꺅  냥[냥]꺅
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            new Phoneme { phoneme = $"{endSoundLastConsonant}",
                            position = totalDuration - Math.Min(totalDuration / 8, vcLengthShort)},
                        }// -음소 있이 이어줌
                        };
                            }
                            
                        }
                        
                    }
                    else{
                        // 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]꺄
                        if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[악]꺅
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)}
                            }
                        };
                        }
                        else{
                            // 앞에 받침이 온 모음 + 모음 아님  냥[악]꺅  냥[먁]꺅
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength)},
                            }
                        };
                        }
                        
                    }
                    }
                    else if ((nextNeighbour?.lyric == "-") || (nextNeighbour?.lyric == "R")){
                        // 둘다 이웃 있고 뒤에 -가 옴
                        if (thisLastConsonant.Equals("")){ // 둘다 이웃 있고 받침 없음 / 냥[냐]냥
                        if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[아]냐
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            }
                        };
                        }
                        else {
                            // 앞에 받침 있는 모음 + 모음 아님 / 냐[냐]냐  냥[아]냐
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            }
                        };
                        }
                        
                    }
                    else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m"))){
                        // 둘다 이웃 있고 받침 있음 - ㄴㄹㅇㅁ / 냐[냥]냐
                        if ((nextFirstConsonant.Equals("n")) || (nextFirstConsonant.Equals("r")) || (nextFirstConsonant.Equals("")) || (nextFirstConsonant.Equals("m"))){
                            // 다음 음소가 ㄴㅇㄹㅇ 임
                            if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[앙]냐
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength),}
                            }
                        };
                        }
                        else{
                            // 앞에 받침이 있는 모음 + 모음 아님 / 냥[앙]냐 냥[냥]냥
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength),}
                            }// -음소 없이 이어줌
                        };
                        }
                            
                        }
                        else{
                            // 다음 음소가 ㄴㅇㄹㅁ 제외 나머지임
                            if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[앙]꺅
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength),}
                            }
                        };
                        }
                            else{
                                // 앞에 받침 있는 모음 + 모음 아님 / 냥[앙]꺅  냥[냥]꺅
                                return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 2, cVCLength),}
                            }
                        };
                            }
                            
                        }
                        
                    }
                    else{
                        // 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]꺄
                        if ((prevLastConsonant.Equals("")) && (thisFirstConsonant.Equals("")) && (thisVowelHead.Equals(""))){
                            // 앞에 받침 없는 모음 / 냐[악]꺅
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{VV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                            }
                        };
                        }
                        else{
                            // 앞에 받침이 온 모음 + 모음 아님  냥[악]꺅  냥[먁]꺅
                            return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                            }
                        };
                        }
                        
                    }
                    }
                    else{
                        if (thisLastConsonant.Equals("")){ // 둘다 이웃 있고 받침 없음 / 냥[냐]-
                        return new Result(){
                        phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                        }
                        };
                    }
                        else if ((thisLastConsonant.Equals("n")) || (thisLastConsonant.Equals("l")) || (thisLastConsonant.Equals("ng")) || (thisLastConsonant.Equals("m"))){
                            // 둘다 이웃 있고 받침 있음 - ㄴㄹㅇㅁ / 냐[냥]-
                        
                            return new Result(){
                            phonemes = new Phoneme[] { 
                            new Phoneme { phoneme = $"{CV}"},
                            new Phoneme { phoneme = $"{cVC}",
                            position = totalDuration - Math.Min(totalDuration / 3, vcLength)},
                                }// -음소 없이 이어줌
                            };
                        }
                        else{
                            // 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]-
                            return new Result(){
                            phonemes = new Phoneme[] { 
                                new Phoneme { phoneme = $"{CV}"},
                                new Phoneme { phoneme = $"{cVC}",
                                position = totalDuration - Math.Min(totalDuration / 3, vcLengthShort)},
                                }
                            };
                        }
                    }
                }
                else{
                    return new Result(){
                    phonemes = new Phoneme[] { 
                            new Phoneme {phoneme = CV},
                            }
                };
                }
            }
            else{
                
                 // TODO 로마자 음소입력 구현
                string phonemeToReturn = lyric; // 아래에서 아무것도 안 걸리면 그냥 가사 반환

                if (thisNote.lyric == "-"){
                    if (hanguel.isHangeul(prevNote?.lyric)){
                        cvPhonemes = cv.convertForCV(prevNote, thisNote, nextNote, new bool[]{isUsingShi, isUsing_aX, isUsing_i, isRentan}); // [isUsingShi], isUsing_aX, isUsing_i, isRentan
                
                        string prevVowelTail = (string)cvPhonemes[2]; // V이전 노트의 모음 음소 
                        string prevLastConsonant = (string)cvPhonemes[3]; // 이전 노트의 받침 음소

                        // 앞 노트가 한글
                        if (! prevLastConsonant.Equals("")){
                            phonemeToReturn = $"{prevLastConsonant} -";
                        }
                        else if (! prevVowelTail.Equals("")){
                            phonemeToReturn = $"{prevVowelTail} -";
                        }

                    }
                    return new Result(){
                    phonemes = new Phoneme[] { 
                            new Phoneme {phoneme = phonemeToReturn},
                            }
                };
                }
                else if (thisNote.lyric == "R"){
                    if (hanguel.isHangeul(prevNote?.lyric)){
                        cvPhonemes = cv.convertForCV(prevNote, thisNote, nextNote, new bool[]{isUsingShi, isUsing_aX, isUsing_i, isRentan}); // [isUsingShi], isUsing_aX, isUsing_i, isRentan
                
                        string prevVowelTail = (string)cvPhonemes[2]; // V이전 노트의 모음 음소 
                        string prevLastConsonant = (string)cvPhonemes[3]; // 이전 노트의 받침 음소

                        // 앞 노트가 한글
                        if (! prevLastConsonant.Equals("")){
                            phonemeToReturn = $"{prevLastConsonant} R";
                        }
                        else if (! prevVowelTail.Equals("")){
                            phonemeToReturn = $"{prevVowelTail} R";
                        }
                        
                    }
                    return new Result(){
                    phonemes = new Phoneme[] { 
                            new Phoneme {phoneme = phonemeToReturn},
                            }
                };
                }
                else{
                return new Result(){
                    phonemes = new Phoneme[] { 
                            new Phoneme {phoneme = phonemeToReturn},
                            }
                };
                }
                
            }
            
           
        }



            
           
        }
        
    }

    
    
