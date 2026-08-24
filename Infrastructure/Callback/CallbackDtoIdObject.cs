using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraBot.Infrastructure.Callback
{
    internal class CallbackDtoIdObject : CallbackDto
    {
        public Guid? ObjectID { get; set; }

        public static new CallbackDtoIdObject FromString(string input)
        {
            var splitArray = input.Split("|");
            var result = new CallbackDtoIdObject();
            result.Action = input.Split("|")[0];
            if (splitArray.Length > 1 && splitArray[1] != string.Empty)
                if (Guid.TryParse(splitArray[1], out var guid))
                    result.ObjectID = guid;
                else result.ObjectID = null;
            else result.ObjectID = null;

            return result;
        }
        public override string ToString() => $"{base.ToString()}|{ObjectID}";

        
    }
}
