using DiscordIrcBridge.Configuration;
using IrcDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Transports.Irc
{
    public class BridgeBot : StandardIrcClient, IDisposable
    {
        private bool _isDisposed = false;
        private readonly IrcConfiguration _configuration;
        private readonly ILogger<BridgeBot> _log;
        public BridgeBot(IrcConfiguration configuration, ILogger<BridgeBot> log)
        {
            _configuration = configuration;
            _log = log;
        }
        ~BridgeBot()
        {
            Dispose(false);
        }

        public void Connect()
        {
            var registrationInfo = new BridgeBotRegistration();
            registrationInfo.NickName = _configuration.Nickname;
            registrationInfo.Password = _configuration.Password;
            registrationInfo.UserName = _configuration.Username;
            registrationInfo.RealName = _configuration.RealName;
            _log.LogInformation($"Connecting to {_configuration.Server}:{_configuration.Port} as {_configuration.Nickname}!{_configuration.Username} / {_configuration.RealName}");
            this.Connect(_configuration.Server, _configuration.Port, false, registrationInfo);
        }
        public new void Disconnect()
        {
            this.Quit(_configuration.ClientQuitTimeout, ":" + _configuration.QuitMessage);
        }

        public new void Dispose()
        {
            Dispose(true);
            base.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Quit()
        {
            Quit(_configuration.ClientQuitTimeout, _configuration.QuitMessage);
        }

        protected override void Dispose(bool disposing)
        {
            if (!this._isDisposed)
            {
                if (disposing)
                {
                    Quit();
                }
                
            }
            base.Dispose(disposing);
            this._isDisposed = true;
        }
    }
}
