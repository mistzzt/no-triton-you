﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CourseIdList = System.Collections.Generic.IReadOnlyList<TritonKinshi.Core.CourseId>;
using SubjectList = System.Collections.Generic.IReadOnlyList<TritonKinshi.Core.Subject>;
using TermList = System.Collections.Generic.IReadOnlyList<TritonKinshi.Core.Term>;
using SectionList = System.Collections.Generic.IReadOnlyList<TritonKinshi.Core.CourseSectionInfo>;

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
        /// Can be called without <see cref="SetTermAsync"/> first.
        /// </summary>
        /// <returns>Available terms currently.</returns>
        Task<TermList> GetTermsAsync();

        /// <summary>
        /// Checks whether current session of <see cref="IWebRegImpl"/> instance is valid.
        /// </summary>
        /// <param name="term">Term of pending operations.</param>
        /// <param name="logged">Whether `start-term` is logged</param>
        /// <returns>true if credentials are valid and the term is available.</returns>
        Task<bool> CheckEligibilityAsync(Term term, bool logged = true);

        Task<SubjectList> SearchSubjectListAsync(Term term);

        Task<CourseIdList> GetPreAuthInfoAsync(Term term);

        Task<CourseIdList> SearchCourseListAsync(Term term, Subject subject);

        Task<SectionList> GetClassAsync(Term term);

        Task<IReadOnlyList<(int order, CourseId course)>> GetPrerequisitesAsync(Term term, CourseId course);

        Task<bool> AddEnrollAsync(Course course);

        Task<bool> DropEnrollAsync(Course course);

        Task<Course> EditEnrollAsync(Course course);

        Task<bool> SendEmailAsync(Term term, string content);

        Task<bool> AddWaitAsync(Course course);

        Task<bool> DropWaitAsync(Course course);

        Task<Course> EditWaitAsync(Course course);

        Task<(DateTime start, DateTime end)> GetEnrollAddDateAsync(Term term);

        Task<PassMessage> GetPassMessageAsync(Term term);

        Task<string> SearchCatelogAsync(CourseId course, Term term);

        Task<IReadOnlyList<string>> SearchRestrictionAsync(CourseId course, Term term);

        Task<IReadOnlyList<(string text, string courseId)>> SearchCourseTextAsync(Subject subject, Term term);

        Task<SectionList> SearchGroupDataAsync(CourseId course, Term term);

        Task GetStatusStartAsync(Term term);

        Task SetTermAsync(Term term);

        /// <summary>
        /// Updates credentials of current <see cref="IWebRegImpl"/> instance.
        /// </summary>
        /// <param name="sso">The provider with valid credentials.</param>
        void UpdateCredentials(ISsoCredentialProvider sso);
    }
}