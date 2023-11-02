using System;
using System.IO;
using Microsoft.ML.OnnxRuntime;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core.DiffSinger {
    public class DsVocoder {
        public string Location;
        public DsVocoderConfig config;
        public InferenceSession session;

        string nsf_hifigan_version = "0.0.0.0";

        //Get vocoder by package name
        public DsVocoder(string name) {
            byte[] model;
            try {
                Location = Path.Combine(PathManager.Inst.DependencyPath, name);
                config = Core.Yaml.DefaultDeserializer.Deserialize<DsVocoderConfig>(
                        File.ReadAllText(Path.Combine(Location, "vocoder.yaml"),
                        System.Text.Encoding.UTF8));
                model = File.ReadAllBytes(Path.Combine(Location, config.model));
            }
            catch (Exception ex) {
                // For better user experience, directly downloads and installs vocoder from (https://github.com/xunmengshe/OpenUtau/wiki/Vocoders), instead of showing error message.
                string oudepPath = Path.Combine(PathManager.Inst.CachePath, "nsf_hifigan.oudep");
                Log.Information("Diffsinger vocoder not exists, automatically installs \"nsf_hifigan\".");

                WebFileDownloader.DownLoadFileAsyncInCache($"https://github.com/xunmengshe/OpenUtau/releases/download/{nsf_hifigan_version}/nsf_hifigan.oudep", "nsf_hifigan.oudep").Wait();
                
                try{
                    DependencyInstaller.Install(oudepPath);

                    Location = Path.Combine(PathManager.Inst.DependencyPath, name);
                    config = Core.Yaml.DefaultDeserializer.Deserialize<DsVocoderConfig>(
                            File.ReadAllText(Path.Combine(Location, "vocoder.yaml"),
                            System.Text.Encoding.UTF8));
                    model = File.ReadAllBytes(Path.Combine(Location, config.model));
                }
                catch{
                    throw new Exception($"Failed to download vocoder {name}. You can download vocoder manually from https://github.com/xunmengshe/OpenUtau/wiki/Vocoders and put it to \"Install Singer\"."); 
                }

                // deletes oudep file in Cache folder.
                if (File.Exists(oudepPath)){
                    try {
                        File.Delete(oudepPath);
                        Log.Information($"successfully deleted {oudepPath}.");
                        }
                    catch (Exception e) {
                        Log.Error($"failed to delete {oudepPath}: {0}", e.Message);
                        }
                }
                else {
                    Log.Error($"{oudepPath} not exists.");
                }
            }
            session = Onnx.getInferenceSession(model);
        }

        public float frameMs() {
            return 1000f * config.hop_size / config.sample_rate;
        }
    }

    [Serializable]
    public class DsVocoderConfig {
        public string name = "vocoder";
        public string model = "model.onnx";
        public int num_mel_bins = 128;
        public int hop_size = 512;
        public int sample_rate = 44100;
    }
}
