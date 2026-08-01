using InfraBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraBot.HelpData
{
    internal class CommndsData
    {
        internal string Description { get; set; } = null!;
        internal List<UserStatus> Levels { get; set; } = [];
    }
}
