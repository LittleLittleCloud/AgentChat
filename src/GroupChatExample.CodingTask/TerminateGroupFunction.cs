using GroupChatExample.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupChatExample.CodingTask
{
    public partial class AdminFunction
    {
        /// <summary>
        /// task complete successfully, terminate the chat.
        /// </summary>
        [FunctionAttribution]
        public async Task<string> TaskCompletedSuccessfully(string terminateMessage)
        {
            return "[TERMINATE]";
        }

        [FunctionAttribution]
        public async Task<string> AskEngineerToFixBug(string task)
        {
            return "Engineer, please fix the bug.";
        }
    }
}
