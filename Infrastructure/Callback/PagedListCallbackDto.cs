using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraBot.Infrastructure.Callback
{
    internal class PagedListCallbackDto : CallbackDtoIdObject
    {
        public int Page { get; set; }

        public static new PagedListCallbackDto FromString(string input)
        {
            var splitInput = input.Split('|');
            var pagedListCallbackDto = new PagedListCallbackDto();
            pagedListCallbackDto.Action = splitInput.Length == 1 ? input : splitInput[0];
            if (splitInput.Length > 1 && splitInput[1] != string.Empty)
            {
                if (Guid.TryParse(splitInput[1], out var guid))
                    pagedListCallbackDto.ObjectID = guid;
            }
            pagedListCallbackDto.Page = splitInput.Length > 2 ? Convert.ToInt32(splitInput[2]) : 0;

            return pagedListCallbackDto;
        }

        public override string ToString()
        {
            return $"{base.ToString()}|{Page}";
        }
    }

    internal class PagedListCallbackDtoServers : PagedListCallbackDto
    {
        public static new PagedListCallbackDtoServers FromString(string input)
        {
            var splitInput = input.Split('|');
            var pagedListCallbackDto = new PagedListCallbackDtoServers();
            pagedListCallbackDto.Action = splitInput.Length == 1 ? input : splitInput[0];
            if (splitInput.Length > 1 && splitInput[1] != string.Empty)
            {
                if (Guid.TryParse(splitInput[1], out var guid))
                    pagedListCallbackDto.ObjectID = guid;
            }
            pagedListCallbackDto.Page = splitInput.Length > 2 ? Convert.ToInt32(splitInput[2]) : 0;

            return pagedListCallbackDto;
        }
    }

    internal class PagedListCallbackDtoUsers : PagedListCallbackDto
    {
        public static new PagedListCallbackDtoUsers FromString(string input)
        {
            var splitInput = input.Split('|');
            var pagedListCallbackDto = new PagedListCallbackDtoUsers();
            pagedListCallbackDto.Action = splitInput.Length == 1 ? input : splitInput[0];
            if (splitInput.Length > 1 && splitInput[1] != string.Empty)
            {
                if (Guid.TryParse(splitInput[1], out var guid))
                    pagedListCallbackDto.ObjectID = guid;
            }
            pagedListCallbackDto.Page = splitInput.Length > 2 ? Convert.ToInt32(splitInput[2]) : 0;

            return pagedListCallbackDto;
        }
    }

    internal class PagedListCallbackDtoScripts : PagedListCallbackDto
    {
        public static new PagedListCallbackDtoScripts FromString(string input)
        {
            var splitInput = input.Split('|');
            var pagedListCallbackDto = new PagedListCallbackDtoScripts();
            pagedListCallbackDto.Action = splitInput.Length == 1 ? input : splitInput[0];
            if (splitInput.Length > 1 && splitInput[1] != string.Empty)
            {
                if (Guid.TryParse(splitInput[1], out var guid))
                    pagedListCallbackDto.ObjectID = guid;
            }
            pagedListCallbackDto.Page = splitInput.Length > 2 ? Convert.ToInt32(splitInput[2]) : 0;

            return pagedListCallbackDto;
        }
    }

    internal class PagedListCallbackDtoSvcAccounts : PagedListCallbackDto
    {
        public static new PagedListCallbackDtoSvcAccounts FromString(string input)
        {
            var splitInput = input.Split('|');
            var pagedListCallbackDto = new PagedListCallbackDtoSvcAccounts();
            pagedListCallbackDto.Action = splitInput.Length == 1 ? input : splitInput[0];
            if (splitInput.Length > 1 && splitInput[1] != string.Empty)
            {
                if (Guid.TryParse(splitInput[1], out var guid))
                    pagedListCallbackDto.ObjectID = guid;
            }
            pagedListCallbackDto.Page = splitInput.Length > 2 ? Convert.ToInt32(splitInput[2]) : 0;

            return pagedListCallbackDto;
        }
    }
}
