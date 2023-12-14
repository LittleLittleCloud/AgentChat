using AgentChat.Example.Share;
using AgentChat.OpenAI;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AgentChat.Core.Tests
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

        [ApiKeyFact("AZURE_OPENAI_API_KEY")]
        [Trait("Category", "openai")]
        public async Task MathChat_End_To_End_Test()
        {
            var teacher = Constant.GPT35.CreateAgent(
                name: "Teacher",
                roleInformation: $@"You act as a preschool math teacher. Here's your workflow in pseudo code:
-workflow-
create_math_question
if answer is correct
    answer_is_correct
else
    say 'try again'
-end-
",
                functionMap: new Dictionary<Azure.AI.OpenAI.FunctionDefinition, Func<string, Task<string>>>
                {
                    { this.CreateMathQuestionFunction, this.CreateMathQuestionWrapper },
                    { this.AnswerIsCorrectFunction, this.AnswerIsCorrectWrapper },
                });

            var student = Constant.GPT35.CreateAgent(
                name: "Student",
                roleInformation: $@"You act as a preschool student. Here's your workflow in pseudo code:
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
                functionMap: new Dictionary<Azure.AI.OpenAI.FunctionDefinition, Func<string, Task<string>>>
                {
                    { this.AnswerQuestionFunction, this.AnswerQuestionWrapper }
                });

            var groupChatFunction = new GroupChatFunction();
            var admin = Constant.GPT35.CreateAgent(
                name: "Admin",
                roleInformation: $@"You act as an admin. Here's your workflow:
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
",
                temperature: 0)
                .CreateAutoReplyAgent("Admin", async (msgs, ct) =>
                {
                    // check if student successfully resolve 5 math problems
                    if (msgs.Where(m => m.From == teacher.Name && m.Content?.Contains("[ANSWER_IS_CORRECT]") is true).Count() >= 5)
                    {
                        return new Message(Role.Assistant, IChatMessageExtension.TERMINATE, from: "Admin");
                    }

                    return null;
                });

            var group = new GroupChat(
                Constant.GPT35,
                admin,
                new[]
                {
                    teacher,
                    student,
                });

            admin.AddInitializeMessage($@"Welcome to the group chat!", group);
            teacher.AddInitializeMessage($@"Hey I'm Teacher", group);
            student.AddInitializeMessage($@"Hey I'm Student", group);
            admin.AddInitializeMessage(@$"Here's the workflow for this group chat:
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
", group);
            var chatHistory = await admin.SendMessageToGroupAsync(group, "the number of resolved question is 0", 30, false);

            // print chat history
            foreach (var message in chatHistory)
            {
                _output.WriteLine(message.FormatMessage());
            }

            // check if there's five questions from teacher
            chatHistory.Where(msg => msg.From == teacher.Name && msg.Content?.Contains("[MATH_QUESTION]") is true)
                    .Count()
                    .Should().Be(5);

            // check if there's more than five answers from student (answer might be wrong)
            chatHistory.Where(msg => msg.From == student.Name && msg.Content?.Contains("[MATH_ANSWER]") is true)
                    .Count()
                    .Should().BeGreaterThanOrEqualTo(5);

            // check if there's five answer_is_correct from teacher
            chatHistory.Where(msg => msg.From == teacher.Name && msg.Content?.Contains("[ANSWER_IS_CORRECT]") is true)
                    .Count()
                    .Should().Be(5);

            // check if there's terminate chat message from admin
            chatHistory.Where(msg => msg.From == admin.Name && msg.IsGroupChatTerminateMessage())
                    .Count()
                    .Should().Be(1);
        }
    }
}
