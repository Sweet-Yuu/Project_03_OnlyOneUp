using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unity.Services.Vivox
{
    internal class JSONModel
    {
        [Serializable]
        internal class JSONParticipant
        {
            public JSONModel.Account account;
            public bool inAudio;
            public bool inText;
            public bool isSelf;
            public int localMute;
            public JSONModel.Parentchannelsession parentChannelSession;
            public string participantId;
            public string role;
        }

        [Serializable]
        internal class Account
        {
            public string issuer;
            public string name;
            public string domain;
            public string environmentId;
        }

        [Serializable]
        internal class Parentchannelsession
        {
            public Channel channel;
            public string _accountJid;
            public string _channelJid;
        }

        [Serializable]
        internal class Channel
        {
            public string domain;
            public string issuer;
            public string name;
            public string type;
        }
    }
}
