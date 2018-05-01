using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using CourseIdList = System.Collections.Immutable.ImmutableList<TritonKinshi.Core.CourseId>;
using SubjectList = System.Collections.Immutable.ImmutableList<TritonKinshi.Core.Subject>;
using TermList = System.Collections.Immutable.ImmutableList<TritonKinshi.Core.Term>;

namespace TritonKinshi.Core
{
    /// <inheritdoc />
    /// <summary>
    /// Provides a low-level implementation of the WebReg system.
    /// </summary>
    public interface IWebRegImpl : IDisposable
    {
        /// <summary>
        /// Get a list of available terms.
        /// Can be called without check <see cref="CheckEligibilityAsync"/> first.
        /// </summary>
        /// <returns>Available terms currently.</returns>
        Task<TermList> GetTermsAsync();

        /// <summary>
        /// Checks whether current session of <see cref="IWebRegImpl"/> instance is valid.
        /// </summary>
        /// <param name="term">Term of pending operations.</param>
        /// <returns>true if credentials are valid and the term is available.</returns>
        Task<bool> CheckEligibilityAsync(Term term);

        Task<SubjectList> SearchSubjectListAsync(Term term);

        Task<CourseIdList> GetPreAuthInfoAsync(Term term);

        Task<CourseIdList> SearchCourseListAsync(Term term, Subject subject);

        Task<CourseIdList> GetEnrolledCoursesAsync(Term term);

        Task<ImmutableList<(int order, CourseId course)>> GetPrerequisitesAsync(Term term, CourseId course);

        Task<bool> AddEnrollAsync(Course course);

        Task<bool> DropEnrollAsync(Course course);

        Task<Course> EditEnrollAsync(Course course);

        Task<bool> SendEmailAsync(Term term, string content);

        Task<bool> AddWaitAsync(Course course);

        Task<bool> DropWaitAsync(Course course);

        Task<Course> EditWaitAsync(Course course);

        Task<(DateTime start, DateTime end)> GetEnrollAddDateAsync(Term term);

        Task<PassMessage> GetPassMessageAsync(Term term);

        /// <summary>
        /// Updates credentials of current <see cref="IWebRegImpl"/> instance.
        /// </summary>
        /// <param name="sso">The provider with valid credentials.</param>
        void UpdateCredentials(ISsoCredentialProvider sso);
    }
}