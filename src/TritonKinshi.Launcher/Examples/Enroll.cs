using System.Linq;
using System.Threading.Tasks;
using TritonKinshi.Core;

namespace TritonKinshi.Launcher.Examples
{
    internal static class Enroll
    {
        public static async Task TestEnroll(IWebRegImpl webReg)
        {
            // load terms and select Summer Session I 2018
            var terms = await webReg.GetTermsAsync();
            var s118 = terms.Single(x => x.Code == "S118");

            await webReg.SetTermAsync(s118);

            // load subjects
            var subjects = await webReg.SearchSubjectListAsync(s118);
            var mae = subjects.Single(x => x.Code == "MAE");

            // load all courses of subject MAE
            var courses = await webReg.SearchCourseListAsync(s118, mae);

            // get mae8 id
            // the course string for MAE 8 contains a space
            // if we remove this space, `search-load-group-data` won't work
            var mae8Id = courses.Single(x => x.Code.Trim() == "8");

            // get all sections
            var sections = await webReg.SearchGroupDataAsync(mae8Id, s118);

            // give the section id we want to enroll
            mae8Id.Section = sections.First(x => x.FK_CDI_INSTR_TYPE == "DI").SECTION_NUMBER;

            // set term and grading option
            var mae8 = new Course
            {
                Id = mae8Id,
                TermCode = s118.Code,
                GradingOption = GradingOption.Letter
            };

            // get default unit and set 'preparing to enroll' status
            mae8 = await webReg.EditEnrollAsync(mae8);

            // add enroll
            await webReg.AddEnrollAsync(mae8);

            // send mail indicating success
            await webReg.SendEmailAsync(s118, "Successfully enrolled.");
        }
    }
}
