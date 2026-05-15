using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using RTLSDR.Common;

namespace RadI0
{
    /// <summary>
    /// The app params.
    /// </summary>
    public class AppParams
    {
        public AppParams(string appName)
        {
            _appName = appName;
        }

        public RaidI0Config Config { get; set; } = new RaidI0Config();

        private readonly string? _appName;

        /// <summary>
        /// Gets or sets a value indicating whether false.
        /// </summary>
        public bool Help { get; set; } = false;

        /// <summary>
        /// Gets or sets the InputFileName.
        /// </summary>
        public string? InputFileName { get; set; } = null;
        /// <summary>
        /// Gets or sets the WaveFileName.
        /// </summary>
        public string? WaveFileName { get; set; } = null;
        /// <summary>
        /// Gets or sets the AACFileName.
        /// </summary>
        public string? AACFileName { get; set; } = null;

        /// <summary>
        /// UDP stream url
        /// </summary>
        public string? UDP { get; set; } = null;

        /// <summary>
        /// Stat UDP stream url
        /// </summary>
        public string? StatUDP { get; set; } = null;

        /// <summary>
        /// Gets or sets the OutputRawFileName.
        /// </summary>
        public string? OutputRawFileName { get; set; } = null;

        /// <summary>
        /// The input source enum.
        /// </summary>
        public InputSourceEnum InputSource = InputSourceEnum.Unknown;

        private bool _FMCommandLineParamSet = false;
        private bool _DABCommandLineParamSet = false;
        private bool _hwgainCommandLineParamSet = false;
        private bool _swgainCommandLineParamSet = false;
        private bool _monoCommandLineParamSet = false;
        private bool _sampleRateCommandLineParamSet = false;
        private bool _frequencyCommandLineParamSet = false;
        private bool _gainCommandLineParamSet = false;
        private bool _serviceNumberCommandLineParamSet = false;



        private string AppName
        {
            get
            {
                if (!String.IsNullOrEmpty(_appName))
                {
                    return _appName;
                }

                if (!String.IsNullOrEmpty(Assembly.GetExecutingAssembly().GetName().Name))
                {
                    return Assembly.GetExecutingAssembly().GetName().Name ?? "RadI0";
                }

                return "RadI0";
            }
        }

        public bool OutputToFile
        {
            get
            {
                return !String.IsNullOrEmpty(WaveFileName)
                || !String.IsNullOrEmpty(AACFileName)
                || !String.IsNullOrEmpty(OutputRawFileName);
            }
        }

        public bool FMCommandLineParamSet { get => _FMCommandLineParamSet; }
        public bool DABCommandLineParamSet { get => _DABCommandLineParamSet; }
        public bool HwgainCommandLineParamSet { get => _hwgainCommandLineParamSet; }
        public bool SwgainCommandLineParamSet { get => _swgainCommandLineParamSet; }
        public bool MonoCommandLineParamSet { get => _monoCommandLineParamSet;  }
        public bool SampleRateCommandLineParamSet { get => _sampleRateCommandLineParamSet; }
        public bool FrequencyCommandLineParamSet { get => _frequencyCommandLineParamSet;}
        public bool GainCommandLineParamSet { get => _gainCommandLineParamSet; }
        public bool ServiceNumberCommandLineParamSet { get => _serviceNumberCommandLineParamSet; }

        public void ShowError(string text)
        {
            System.Console.WriteLine($"Error. {text}. See help:");
            System.Console.WriteLine();
            System.Console.WriteLine($"{AppName} -help");
            System.Console.WriteLine();
        }

        public void ShowHelp()
        {
            var asmPath = Assembly.GetExecutingAssembly().Location;
            var appDir = Path.GetDirectoryName(asmPath);
            var helpFileName = Path.Join(appDir, "help.txt");

            System.Console.WriteLine(File.ReadAllText(helpFileName));
        }

        public bool ParseArgs(string[] args)
        {
            InputSource = InputSourceEnum.Unknown;

            var valueExpecting = false;
            string? valueExpectingParamName = null;
            var notDescribedParamsCount = 0;

            foreach (var arg in args)
            {
                var p = arg.ToLower().Trim();
                if (p.StartsWith("--", StringComparison.InvariantCulture))
                {
                    p = p.Substring(1);
                }

                if (p.StartsWith("-") && (!valueExpecting))
                {
                    if (valueExpecting)
                    {
                        ShowError($"Expecting param: {valueExpectingParamName}");
                        return false;
                    }

                    switch (p.Substring(1))
                    {
                        case "help":
                            Help = true;
                            break;
                        case "fm":
                            Config.FM = true;
                            _FMCommandLineParamSet = true;
                            break;
                        case "hg":
                        case "hgain":
                        case "hwgain":
                            Config.HWGain = true;
                            Config.SWGain = false;
                            _hwgainCommandLineParamSet = true;
                            break;
                        case "sg":
                        case "sgain":
                        case "swgain":
                            Config.SWGain = true;
                            Config.HWGain = false;
                            _swgainCommandLineParamSet = true;
                            break;
                        case "dab":
                        case "dab+":
                            Config.DAB = true;
                            _DABCommandLineParamSet = true;
                            break;
                        case "mono":
                            Config.Mono = true;
                            _monoCommandLineParamSet = true;
                            break;
                        case "if":
                        case "ifile":
                        case "infile":
                        case "inputfile":
                        case "ifilename":
                        case "infilename":
                        case "inputfilename":
                            valueExpecting = true;
                            valueExpectingParamName = "ifile";
                            break;
                        case "wave":
                            valueExpecting = true;
                            valueExpectingParamName = "wave";
                            break;
                        case "aac":
                            valueExpecting = true;
                            valueExpectingParamName = "aac";
                            break;
                        case "udp":
                            valueExpecting = true;
                            valueExpectingParamName = "udp";
                            break;
                        case "stat":
                        case "statudp":
                            valueExpecting = true;
                            valueExpectingParamName = "statudp";
                            break;
                        case "sn":
                        case "snumber":
                        case "servicenumber":
                            valueExpecting = true;
                            valueExpectingParamName = "sn";
                            _serviceNumberCommandLineParamSet = true;
                            break;
                        case "f":
                        case "freq":
                        case "frequency":
                            valueExpecting = true;
                            valueExpectingParamName = "f";
                            _frequencyCommandLineParamSet = true;
                            break;
                        case "g":
                        case "gain":
                            valueExpecting = true;
                            valueExpectingParamName = "g";
                            _gainCommandLineParamSet = true;
                            break;
                        default:
                            ShowError($"Unknown param: {p}");
                            return false;
                    }
                }
                else
                {
                    if (valueExpecting)
                    {
                        switch (valueExpectingParamName)
                        {
                            case "ifile":
                                InputFileName = arg;
                                InputSource = InputSourceEnum.File;
                                break;
                            case "wave":
                                WaveFileName = arg;
                                break;
                            case "aac":
                                AACFileName = arg;
                                break;
                            case "udp":
                                UDP = arg;
                                break;
                            case "statudp":
                                StatUDP = arg;
                                break;
                            case "orawfile":
                                OutputRawFileName = arg;
                                break;
                            case "f":
                                var freq = AudioTools.ParseFreq(arg);
                                if (freq>0)
                                {
                                    Config.Frequency = freq;
                                } else
                                {
                                    ShowError($"Param error: {valueExpectingParamName}");
                                    return false;
                                }
                                break;
                            case "g":
                                int g;

                                if (!int.TryParse(arg, out g))
                                {
                                    ShowError($"Param error: {valueExpectingParamName}");
                                    return false;
                                }
                                Config.Gain = g;
                                Config.SWGain = false;
                                Config.HWGain = false;
                                break;
                            case "sn":
                                int sn;
                                if (!int.TryParse(arg, out sn))
                                {
                                    ShowError($"Param error: {valueExpectingParamName}");
                                    return false;
                                }
                                Config.ServiceNumber = sn;
                                break;
                            default:
                                ShowError($"Unexpected param: {valueExpectingParamName}");
                                return false;
                        }

                        valueExpecting = false;
                    }
                    else
                    {
                        notDescribedParamsCount++;

                        if (notDescribedParamsCount == 1)
                        {
                            if (String.IsNullOrEmpty(InputFileName))
                            {
                                InputFileName = arg;
                                InputSource = InputSourceEnum.File;
                            }
                            else
                            {
                                ShowError($"Input FileName already specified");
                                return false;
                            }
                        }
                        else
                        {
                            if (notDescribedParamsCount == 2)
                            {
                                if (String.IsNullOrEmpty(WaveFileName))
                                {
                                    WaveFileName = arg;
                                }
                                else
                                {
                                    ShowError($"Output FileName already specified");
                                    return false;
                                }
                            }
                            else
                            {
                                ShowError($"Too many parameters");
                                return false;
                            }
                        }
                    }
                }
            }

           return AutoSetParams();
        }

        private bool AutoSetParams()
        {
            if (Help)
            {
                ShowHelp();
                return false;
            }

            if (InputSource == InputSourceEnum.Unknown)
            {
                InputSource = InputSourceEnum.RTLDevice;
            }

            // DAB 5A default
            if ((!Config.FM && !Config.DAB) && (Config.Frequency <= 0))
            {
                Config.Frequency = AudioTools.DABMinFreq; // 5A
                Config.DAB = true;
                _DABCommandLineParamSet = true;
            }

            // autodetect FM/DAB by frequency
            if ((InputSource == InputSourceEnum.RTLDevice) && (!Config.FM && !Config.DAB) && (Config.Frequency >= 0))
            {
                if (
                    (Config.Frequency>=AudioTools.DABMinFreq) &&
                    (Config.Frequency<=AudioTools.DABMaxFreq)
                   )
                {
                    Config.DAB = true;
                    _DABCommandLineParamSet = true;
                } else
                {
                    if (
                        (Config.Frequency>=AudioTools.FMMinFreq) &&
                        (Config.Frequency<=AudioTools.FMMaxFreq)
                       )
                    {
                        Config.FM = true;
                        _FMCommandLineParamSet = true;
                    } else
                    {
                        System.Console.WriteLine("Missing FM or DAB parameter!");
                        return false;
                    }
                }
            }

            // default freq for FM => 88 MHz, DAB => 5A
            if ((InputSource == InputSourceEnum.RTLDevice) && (Config.Frequency <= 0))
            {
                if (Config.FM)
                {
                    Config.Frequency = AudioTools.FMMinFreq;
                }
                if (Config.DAB)
                {
                    Config.Frequency = AudioTools.DABMinFreq; // 5A
                }
            }

                if (    (Config.FM) &&
                        (
                            (Config.Frequency<AudioTools.FMMinFreq) ||
                            (Config.Frequency>AudioTools.FMMaxFreq)
                        )
                       )
                       {
                            // FM freq out of bounds
                            Config.Frequency = AudioTools.FMMinFreq;
                       }


                if (    (Config.DAB) &&
                        (
                            (Config.Frequency<AudioTools.DABMinFreq) ||
                            (Config.Frequency>AudioTools.DABMaxFreq)
                        )
                       )
                       {
                            // DAB freq out of bounds
                            Config.Frequency = AudioTools.DABMinFreq;
                       }

            return true;
        }

    }
}


