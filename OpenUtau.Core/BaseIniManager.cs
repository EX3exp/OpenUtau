using OpenUtau.Core.Util;
using OpenUtau.Core.Ustx;
        public abstract class BaseIniManager : IniParser{
            protected USinger singer;
            protected IniFile iniFile = new IniFile();
            protected string iniFileName;

            public BaseIniManager() { }


            /// <summary>
            /// if no [iniFileName] in Singer Directory, it makes new [iniFileName] with settings in [iniSetUp(iniFile)].
            /// </summary>
            /// <param name="singer"></param>
            /// <param name="iniFileName"></param>
            public void initialize(USinger singer, string iniFileName) {
                this.singer = singer;
                this.iniFileName = iniFileName;
                try {
                    iniFile.Load($"{singer.Location}/{iniFileName}");
                    iniSetUp(iniFile); // you can override iniSetUp() to use.
                } catch {
                    iniSetUp(iniFile); // you can override iniSetUp() to use.
                }

            }

            /// <summary>
            /// <para>you can override this method with your own values. </para> 
                /// !! when implement this method, you have to use [setOrReadThisValue(string sectionName, string keyName, bool/string/int/double value)] when setting or reading values.
                /// <para>(ex)
                /// setOrReadThisValue("sectionName", "keyName", true);</para>
                /// </summary>
            protected virtual void iniSetUp(IniFile iniFile) {
                
            }

            /// <summary>
            /// <param name="sectionName"> section's name in .ini config file. </param>
            /// <param name="keyName"> key's name in .ini config file's [sectionName] section. </param>
            /// <param name="defaultValue"> default value to overwrite if there's no valid value in config file. </param>
            /// inputs section name & key name & default value. If there's valid boolean vaule, nothing happens. But if there's no valid boolean value, overwrites current value with default value.
            /// 섹션과 키 이름을 입력받고, bool 값이 존재하면 넘어가고 존재하지 않으면 defaultValue 값으로 덮어씌운다 
            /// </summary>
            protected void setOrReadThisValue(string sectionName, string keyName, bool defaultValue) {
                iniFile[sectionName][keyName] = iniFile[sectionName][keyName].ToBool(defaultValue);
                iniFile.Save($"{singer.Location}/{iniFileName}");
            }

            /// <summary>
            /// <param name="sectionName"> section's name in .ini config file. </param>
            /// <param name="keyName"> key's name in .ini config file's [sectionName] section. </param>
            /// <param name="defaultValue"> default value to overwrite if there's no valid value in config file. </param>
            /// inputs section name & key name & default value. If there's valid string vaule, nothing happens. But if there's no valid string value, overwrites current value with default value.
            /// 섹션과 키 이름을 입력받고, string 값이 존재하면 넘어가고 존재하지 않으면 defaultValue 값으로 덮어씌운다 
            /// </summary>
            protected void setOrReadThisValue(string sectionName, string keyName, string defaultValue) {
                if (!iniFile[sectionName].ContainsKey(keyName)) {
                    // 키가 존재하지 않으면 새로 값을 넣는다
                    iniFile[sectionName][keyName] = defaultValue;
                    iniFile.Save($"{singer.Location}/{iniFileName}");
                }
                // 키가 존재하면 그냥 스킵
            }

            /// <summary>
            /// 
            /// <param name="sectionName"> section's name in .ini config file. </param>
            /// <param name="keyName"> key's name in .ini config file's [sectionName] section. </param>
            /// <param name="defaultValue"> default value to overwrite if there's no valid value in config file. </param>
            /// inputs section name & key name & default value. If there's valid int vaule, nothing happens. But if there's no valid int value, overwrites current value with default value.
            /// 섹션과 키 이름을 입력받고, int 값이 존재하면 넘어가고 존재하지 않으면 defaultValue 값으로 덮어씌운다 
            /// </summary>
            protected void setOrReadThisValue(string sectionName, string keyName, int defaultValue) {
                iniFile[sectionName][keyName] = iniFile[sectionName][keyName].ToInt(defaultValue);
                iniFile.Save($"{singer.Location}/{iniFileName}");
            }

            /// <summary>
            /// <param name="sectionName"> section's name in .ini config file. </param>
            /// <param name="keyName"> key's name in .ini config file's [sectionName] section. </param>
            /// <param name="defaultValue"> default value to overwrite if there's no valid value in config file. </param>
            /// inputs section name & key name & default value. If there's valid double vaule, nothing happens. But if there's no valid double value, overwrites current value with default value.
            /// 섹션과 키 이름을 입력받고, double 값이 존재하면 넘어가고 존재하지 않으면 defaultValue 값으로 덮어씌운다 
            /// </summary>
            protected void setOrReadThisValue(string sectionName, string keyName, double defaultValue) {
                iniFile[sectionName][keyName] = iniFile[sectionName][keyName].ToDouble(defaultValue);
                iniFile.Save($"{singer.Location}/{iniFileName}");
            }
        }