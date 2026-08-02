using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraBot.Infrastracture.Callback
{
    internal class CallbackDto
    {
        public string Action { get; set; }
        public static CallbackDto FromString(string input) => new CallbackDto() { Action = input.Split("|")[0] };
        public override string ToString() => Action;
    }
}
