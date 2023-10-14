using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace GroupChatExample.Helper.Tests
{
    public partial class MathClassTest
    {
        private readonly ITestOutputHelper _output;
        public MathClassTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [FunctionAttribution]
        public async Task<string> CreateMathQuestion(string question, int question_index)
        {
            return $@"// ignore this line [MATH_QUESTION]
Question #{question_index}:
{question}";
        }

        [FunctionAttribution]
        public async Task<string> AnswerQuestion(string answer)
        {
            return $@"// ignore this line [MATH_ANSWER]
{answer}";
        }

        [FunctionAttribution]
        public async Task<string> AnswerIsCorrect(string message)
        {
            return $@"// ignore this line [ANSWER_IS_CORRECT]
{message}";
        }

        [ApiKeyFact]
        public async Task MathChat_End_To_End_Test()
        {
            var teacher = new ChatAgent(
                Constant.AzureGPT35,
                Constant.AZURE_GPT_35_MODEL_ID,
                "Teacher",
                $@"You act as a preschool math teacher. Here's your workflow in pseudo code:
-workflow-
create_math_question
if answer is correct
    answer_is_correct
else
    say 'try again'
-end-
",
                new Dictionary<Azure.AI.OpenAI.FunctionDefinition, Func<string, Task<string>>>
                {
                    { this.CreateMathQuestionFunction, this.CreateMathQuestionWrapper },
                    { this.AnswerIsCorrectFunction, this.AnswerIsCorrectWrapper },
                });

            var student = new ChatAgent(
                Constant.AzureGPT35,
                Constant.AZURE_GPT_35_MODEL_ID,
                "Student",
                $@"You act as a preschool student. Here's your workflow in pseudo code:
-workflow-
answer_question
if answer is wrong
    fix_answer
-end-

Here are a few examples of answer_question:
-example 1-
2

Here are a few examples of fix_answer:
-example 1-
sorry, the answer should be 2, not 3
",
                new Dictionary<Azure.AI.OpenAI.FunctionDefinition, Func<string, Task<string>>>
                {
                    { this.AnswerQuestionFunction, this.AnswerQuestionWrapper }
                });
            var admin = new ChatAgent(
                Constant.AzureGPT35,
                Constant.AZURE_GPT_35_MODEL_ID,
                "Admin",
                $@"You act as an admin. Here's your workflow:
-workflow-
if number_of_resolved_question > 5:
    terminate_chat
else
    not_enough_question
-end-

Here are a few examples of terminate_chat:
-example 1-
terminate chat. good bye

Here are a few examples of not_enough_question:
-example 1-
the number of resolved question is 0 and it's smaller than 5, please create a question
");

            var group = new GroupChat(
                Constant.AzureGPT35,
                Constant.AZURE_GPT_35_MODEL_ID,
                admin,
                new[]
                {
                    teacher,
                    student,
                });

            admin.FunctionMaps.Add(group.TerminateGroupChatFunction, group.TerminateGroupChatWrapper);

            group.AddInitializeMessage($@"Welcome to the group chat!", admin.Name);
            group.AddInitializeMessage($@"Hey I'm Teacher", teacher.Name);
            group.AddInitializeMessage($@"Hey I'm Student", student.Name);
            group.AddInitializeMessage(@$"Here's the workflow for this group chat:
-group chat workflow-
number_of_resolved_question = 0
while number_of_resolved_question < 5:
    admin_update_number_of_resolved_question
    teacher_create_math_question
    student_answer_question
    teacher_check_answer
    if answer is wrong:
        student_fix_answer
        
admin_terminate_chat
-end-
", admin.Name);
            var chatHistory = await admin.SendMessageAsync("the number of resolved question is 0", group, 30, false);

            // print chat history
            foreach ((var message, var name) in chatHistory)
            {
                _output.WriteLine(group.FormatMessage(message, name));
            }

            // check if there's five questions from teacher
            chatHistory.Where(msg => msg.Item2 == teacher.Name && msg.Item1.Content.Contains("[MATH_QUESTION]"))
                    .Count()
                    .Should().Be(5);

            // check if there's more than five answers from student (answer might be wrong)
            chatHistory.Where(msg => msg.Item2 == student.Name && msg.Item1.Content.Contains("[MATH_ANSWER]"))
                    .Count()
                    .Should().BeGreaterThanOrEqualTo(5);

            // check if there's five answer_is_correct from teacher
            chatHistory.Where(msg => msg.Item2 == teacher.Name && msg.Item1.Content.Contains("[ANSWER_IS_CORRECT]"))
                    .Count()
                    .Should().Be(5);

            // check if there's terminate chat message from admin
            chatHistory.Where(msg => msg.Item2 == admin.Name && msg.Item1.IsGroupChatTerminateMessage())
                    .Count()
                    .Should().Be(1);
        }
    }
}
