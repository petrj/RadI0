using System;
using System.Collections.Generic;
using LoggerService;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The fig parser.
    /// </summary>
    public class FIGParser
    {
        /// <summary>
        /// Gets or sets the programme service labels.
        /// </summary>
        public List<DABProgrammeServiceLabel> ProgrammeServiceLabels { get; set; } = new List<DABProgrammeServiceLabel>();
        private readonly FIB? _fib = null;
        private readonly List<DABService> _DABServices = new List<DABService>();
        private readonly List<DABService> _DABServicesNotified = new List<DABService>();

        private readonly ILoggingService? _loggingService = null;

        private Dictionary<uint, DABSubChannel> SubChanels { get; set; }
        private Dictionary<uint, DABProgrammeServiceLabel> ServiceLabels { get; set; }

        public List<DABService> DABServices => _DABServices;

        /// <summary>
        /// Occurs when null.
        /// </summary>
        public event EventHandler? OnServiceFound = null;

        public FIGParser(ILoggingService loggingService, FIB fib)
        {
            _fib = fib;
            _DABServices = new List<DABService>();
            _loggingService = loggingService;

            _loggingService.Info("Initializing FIGParser");

            SubChanels = new Dictionary<uint, DABSubChannel>();
            ServiceLabels = new Dictionary<uint, DABProgrammeServiceLabel>();

            _fib.ProgrammeServiceLabelFound += _fib_ProgramServiceLabelFound;
            _fib.EnsembleFound += _fib_EnsembleFound;
            _fib.SubChannelFound += _fib_SubChannelFound;
            _fib.ServiceComponentFound += _fib_ServiceComponentFound;
            _fib.ServiceComponentGlobalDefinitionFound += _fib_ServiceComponentGlobalDefinitionFound;
        }

        public void Clear()
        {
            _DABServices.Clear();
            _DABServicesNotified.Clear();
            SubChanels.Clear();
            ServiceLabels.Clear();
        }

        private DABService? GetServiceByNumber(uint serviceNumber)
        {
            foreach (var service in DABServices)
            {
                if (service.ServiceNumber == serviceNumber)
                {
                    return service;
                }
            }
            return null;
        }

        private DABService? GetServiceBySubChId(uint subChId)
        {
            foreach (var service in DABServices)
            {
                var c = service.GetComponentBySubChId(subChId);
                if (c != null)
                {
                    return service;
                }
            }
            return null;
        }

        private void AfterAnythingChanged()
        {
            foreach (var service in DABServices)
            {
                if (_DABServicesNotified.Contains(service))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(service.ServiceName) ||
                    service.SubChannelsCount == 0)
                    continue;

                if (OnServiceFound != null)
                {
                    OnServiceFound(this, new DABServiceFoundEventArgs()
                    {
                        Service = service
                    });
                }

                _DABServicesNotified.Add(service);
            }
        }

        private void _fib_ServiceComponentGlobalDefinitionFound(object? sender, EventArgs e)
        {
            if (e is ServiceComponentGlobalDefinitionFoundEventArgs gde)
            {
                AfterAnythingChanged();
            }
        }

        private void _fib_EnsembleFound(object? sender, EventArgs e)
        {
            if (e is EnsembleFoundEventArgs ensembleArgs)
            {
                AfterAnythingChanged();
            }
        }

        private void _fib_ProgramServiceLabelFound(object? sender, EventArgs e)
        {
            if (e is ProgrammeServiceLabelFoundEventArgs sla)
            {
                var service = GetServiceByNumber(sla.ProgrammeServiceLabel?.ServiceNumber ?? 0);
                if (service != null)
                {
                    if (string.IsNullOrWhiteSpace(service.ServiceName))
                    {
                        service.ServiceName = sla.ProgrammeServiceLabel?.ServiceLabel ?? string.Empty;
                        AfterAnythingChanged();
                    }
                } else
                {
                    if (!ServiceLabels.ContainsKey(sla.ProgrammeServiceLabel?.ServiceNumber ?? 0))
                    {
                        ServiceLabels.Add(sla.ProgrammeServiceLabel?.ServiceNumber ?? 0,
                                            sla.ProgrammeServiceLabel ?? new DABProgrammeServiceLabel());
                    }
                }
            }
        }

        private void _fib_ServiceComponentFound(object? sender, EventArgs e)
        {
            if (e is ServiceComponentFoundEventArgs serviceArgs)
            {
                var service = GetServiceByNumber(serviceArgs.ServiceComponent?.ServiceNumber ?? 0);
                if (service == null)
                {
                    // adding service
                    service  = new DABService()
                    {
                        ServiceNumber = serviceArgs.ServiceComponent?.ServiceNumber ?? 0,
                        Components = serviceArgs.ServiceComponent?.Components,
                        CountryId = serviceArgs.ServiceComponent?.CountryId,
                        ExtendedCountryCode = serviceArgs.ServiceComponent?.ExtendedCountryCode,
                    };

                    DABServices.Add(service);

                    service.SetSubChannels(SubChanels);
                    service.SetServiceLabels(ServiceLabels);

                    AfterAnythingChanged();
                }
            }
        }

        private void _fib_SubChannelFound(object? sender, EventArgs e)
        {
            if (e is SubChannelFoundEventArgs s)
            {
                var service = GetServiceBySubChId(s.SubChannel?.SubChId ?? 0);
                if (service != null)
                {
                    service.SetSubChannels(new Dictionary<uint, DABSubChannel>()
                        {
                            { s.SubChannel?.SubChId ?? 0, s.SubChannel ?? new DABSubChannel() }
                        });
                    service.SetServiceLabels(ServiceLabels);

                    AfterAnythingChanged();
                } else
                {
                    if (!SubChanels.ContainsKey(s.SubChannel?.SubChId ?? 0))
                    {
                        SubChanels.Add(s.SubChannel?.SubChId ?? 0, s.SubChannel ?? new DABSubChannel());
                    }
                }
            }
        }

        public void ClearServices()
        {
            DABServices.Clear();
            ServiceLabels.Clear();
            SubChanels.Clear();
            _DABServicesNotified.Clear();
        }
    }
}
