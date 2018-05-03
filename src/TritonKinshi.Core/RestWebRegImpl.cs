using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TritonKinshi.Core.Extensions;

using CourseIdList = System.Collections.Generic.IReadOnlyList<TritonKinshi.Core.CourseId>;
using SubjectList = System.Collections.Generic.IReadOnlyList<TritonKinshi.Core.Subject>;
using TermList = System.Collections.Generic.IReadOnlyList<TritonKinshi.Core.Term>;
using SectionList = System.Collections.Generic.IReadOnlyList<TritonKinshi.Core.CourseSectionInfo>;

namespace TritonKinshi.Core
{
    public sealed class RestWebRegImpl : IWebRegImpl
    {
        private readonly HttpClient _client;
        private readonly CookieContainer _container;

        private bool _disposed;

        internal RestWebRegImpl(HttpClient client, CookieContainer container)
        {
            _client = client;
            _container = container;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _client.Dispose();
            _disposed = true;
        }

        public async Task SetTermAsync(Term term)
        {
            await SendWrLoggerAsync();
            await GetStatusStartAsync(term);
            if (await CheckEligibilityAsync(term, true))
            {
                return;
            }

            throw new Exception("set term failed");
        }

        public void UpdateCredentials(ISsoCredentialProvider sso)
        {
            var cookies = sso.GetCredentials();
            _container.Add(cookies);
        }

        public async Task<TermList> GetTermsAsync()
        {
            var terms = await RequestGet<Term[]>(WebRegApi.GetTerm, new NameValueCollection());

            return terms;
        }

        public async Task<bool> CheckEligibilityAsync(Term term, bool logged = false)
        {
            var responseType = new
            {
                OPSIV = string.Empty,
                OPS = string.Empty,
                FLAG1 = 0,
                FLAG0 = 0,
                WARNING = string.Empty
            };

            var response = await RequestGet(WebRegApi.CheckEligibility, responseType, new NameValueCollection
            {
                ["termcode"] = term.Code,
                ["seqid"] = term.SequenceId.ToString(),
                ["logged"] = logged.ToString()
            });

            return response.OPSIV == "SUCCESS";
        }

        public async Task<SubjectList> SearchSubjectListAsync(Term term)
        {
            var subjects = await RequestGet<Subject[]>(WebRegApi.LoadSubject, new NameValueCollection
            {
                ["termcode"] = term.Code
            });

            for (var i = 0; i < subjects.Length; i++)
            {
                subjects[i].Code = subjects[i].Code?.Trim();
            }

            return subjects;
        }

        public async Task<CourseIdList> GetPreAuthInfoAsync(Term term)
        {
            var responseType = new
            {
                DISPLAY = string.Empty,
                LIST_DATA = new[]
                {
                    new
                    {
                        SUBJ_CODE = string.Empty,
                        CRSE_CODE = string.Empty,
                        OVERRIDE_TYPE_1 = string.Empty,
                        OVERRIDE_TYPE_2 = string.Empty,
                        OVERRIDE_TYPE_3 = string.Empty
                    }
                }
            };

            var info = await RequestGet(WebRegApi.GetPreAuth, responseType, new NameValueCollection
            {
                ["termcode"] = term.Code
            });

            return info.LIST_DATA.Select(x => new CourseId
            {
                Subject = x.SUBJ_CODE.Trim(),
                Code = x.CRSE_CODE
            }).ToArray();
        }

        public async Task<CourseIdList> SearchCourseListAsync(Term term, Subject subject)
        {
            var responseType = new[]
            {
                new
                {
                    SUBJ_CODE = string.Empty,
                    CRSE_CODE = string.Empty
                }
            };

            var courses = await RequestGet(WebRegApi.LoadCourses, responseType, new NameValueCollection
            {
                ["termcode"] = term.Code,
                ["subjlist"] = subject.Code
            });

            var list = courses.Select(x => new CourseId
            {
                Subject = subject.Code,
                Code = x.CRSE_CODE
            }).ToArray();

            return list;
        }

        public async Task<SectionList> GetClassAsync(Term term)
        {
            var response = await RequestGet<SectionList>(WebRegApi.GetClass, new NameValueCollection
            {
                ["termcode"] = term.Code,
                ["schedname"] = string.Empty,
                ["final"] = string.Empty,
                ["sectnum"] = string.Empty
            });

            return response;
        }

        public async Task<IReadOnlyList<(int order, CourseId course)>> GetPrerequisitesAsync(Term term, CourseId course)
        {
            var responseType = new[]
            {
                new
                {
                    SUBJECT_CODE = string.Empty,
                    PREREQ_SEQ_ID = 0,
                    CRSE_TITLE = string.Empty,
                    COURSE_CODE = string.Empty,
                    GRADE_SEQ_ID = string.Empty,
                    TYPE = string.Empty
                }
            };

            var response = await RequestGet(WebRegApi.GetPrerequisites, responseType, new NameValueCollection
            {
                ["termcode"] = term.Code,
                ["subjcode"] = course.Subject,
                ["crsecode"] = course.Code
            });

            return response.Select(x => (order: x.PREREQ_SEQ_ID, course: new CourseId
            {
                Code = x.COURSE_CODE.Trim(),
                Subject = x.SUBJECT_CODE.Trim()
            })).ToArray();
        }

        public async Task<bool> AddEnrollAsync(Course course)
        {
            var responseType = new
            {
                OPSIV = string.Empty,
                OPS = string.Empty,
                WARNING = string.Empty
            };

            var response = await RequestPost(WebRegApi.AddEnroll, responseType, new Dictionary<string, string>
            {
                ["termcode"] = course.TermCode,
                ["subjcode"] = course.Id.Subject,
                ["crsecode"] = course.Id.Code,
                ["section"] = course.Id.Section.ToString(),
                ["grade"] = course.GradingOption.ToString()[0].ToString(),
                ["unit"] = course.Unit.ToString("F2")
            });

            return response.OPSIV == response.OPS && response.OPSIV == "SUCCESS";
        }

        public async Task<bool> DropEnrollAsync(Course course)
        {
            var responseType = new
            {
                OPSIV = string.Empty,
                OPS = string.Empty,
                WARNING = string.Empty
            };

            var response = await RequestPost(WebRegApi.DropEnroll, responseType, new Dictionary<string, string>
            {
                ["termcode"] = course.TermCode,
                ["subjcode"] = course.Id.Subject,
                ["crsecode"] = course.Id.Code,
                ["section"] = course.Id.Section.ToString()
            });

            return response.OPSIV == response.OPS && response.OPSIV == "SUCCESS";
        }

        public async Task<Course> EditEnrollAsync(Course course)
        {
            var responseType = new
            {
                OPSIV = string.Empty,
                OPS = string.Empty,
                GRADE = string.Empty,
                UNIT_DEFAULT = 0d,
                GRADE_DEFAULT = string.Empty,
                WARNING = string.Empty
            };

            var response = await RequestPost(WebRegApi.EditEnroll, responseType, new Dictionary<string, string>
            {
                ["termcode"] = course.TermCode,
                ["subjcode"] = course.Id.Subject,
                ["crsecode"] = course.Id.Code,
                ["section"] = course.Id.Section.ToString()
            });

            course.Unit = response.UNIT_DEFAULT;

            return course;
        }

        public async Task<bool> SendEmailAsync(Term term, string content)
        {
            var responseType = new
            {
                SUCCESS = string.Empty,
                MAIL_ADDR = string.Empty
            };

            var response = await RequestPost(WebRegApi.SendEmail, responseType, new Dictionary<string, string>
            {
                ["actionevent"] = content,
                ["termcode"] = term.Code
            });

            return response.SUCCESS == "YES";
        }

        public async Task<bool> AddWaitAsync(Course course)
        {
            var responseType = new
            {
                OPSIV = string.Empty,
                OPS = string.Empty,
                WARNING = string.Empty
            };

            var response = await RequestPost(WebRegApi.AddWait, responseType, new Dictionary<string, string>
            {
                ["termcode"] = course.TermCode,
                ["subjcode"] = course.Id.Subject,
                ["crsecode"] = course.Id.Code,
                ["section"] = course.Id.Section.ToString(),
                ["grade"] = course.GradingOption.ToString()[0].ToString(),
                ["unit"] = course.Unit.ToString("F2")
            });

            return response.OPSIV == response.OPS && response.OPSIV == "SUCCESS";
        }

        public async Task<bool> DropWaitAsync(Course course)
        {
            var responseType = new
            {
                OPSIV = string.Empty,
                OPS = string.Empty,
                WARNING = string.Empty
            };

            var response = await RequestPost(WebRegApi.DropWait, responseType, new Dictionary<string, string>
            {
                ["termcode"] = course.TermCode,
                ["subjcode"] = course.Id.Subject,
                ["crsecode"] = course.Id.Code,
                ["section"] = course.Id.Section.ToString()
            });

            return response.OPSIV == response.OPS && response.OPSIV == "SUCCESS";
        }

        public async Task<Course> EditWaitAsync(Course course)
        {
            var responseType = new
            {
                OPSIV = string.Empty,
                OPS = string.Empty,
                GRADE = string.Empty,
                UNIT_DEFAULT = 0d,
                GRADE_DEFAULT = string.Empty,
                WARNING = string.Empty
            };

            var response = await RequestPost(WebRegApi.EditWait, responseType, new Dictionary<string, string>
            {
                ["termcode"] = course.TermCode,
                ["subjcode"] = course.Id.Subject,
                ["crsecode"] = course.Id.Code,
                ["section"] = course.Id.Section.ToString()
            });

            course.Unit = response.UNIT_DEFAULT;

            return course;
        }

        public async Task<(DateTime start, DateTime end)> GetEnrollAddDateAsync(Term term)
        {
            var responseType = new
            {
                START_DATE = DateTime.MinValue,
                END_DATE = DateTime.MaxValue
            };

            var response = await RequestGet(WebRegApi.GetEnrollAddDate, responseType, new NameValueCollection
            {
                ["termcode"] = term.Code
            });

            return (start: response.START_DATE, end: response.END_DATE);
        }

        public async Task<PassMessage> GetPassMessageAsync(Term term)
        {
            var responseType = new
            {
                APPT_TIMER = string.Empty,
                COLLEGE_CODE = string.Empty,
                DISPLAY = string.Empty,

                FIRST_BEGIN_DATE = string.Empty,
                FIRST_BEGIN_HOUR = string.Empty,
                FIRST_BEGIN_MIN = string.Empty,

                FIRST_END_DATE = string.Empty,
                FIRST_END_HOUR = string.Empty,
                FIRST_END_MIN = string.Empty,

                SECOND_BEGIN_DATE = string.Empty,
                SECOND_BEGIN_HOUR = string.Empty,
                SECOND_BEGIN_MIN = string.Empty,

                SECOND_END_DATE = string.Empty,
                SECOND_END_HOUR = string.Empty,
                SECOND_END_MIN = string.Empty,
            };

            var response = await RequestGet(WebRegApi.GetMessagePass, responseType, new NameValueCollection
            {
                ["termcode"] = term.Code
            });

            return new PassMessage
            {
                AppointmentTimer = response.APPT_TIMER,
                CollegeCode = response.COLLEGE_CODE,
                Display = response.DISPLAY,
                FirstPassBegin = ParseFromString(response.FIRST_BEGIN_DATE, response.FIRST_BEGIN_HOUR, response.FIRST_BEGIN_MIN),
                FirstPassEnd = ParseFromString(response.FIRST_END_DATE, response.FIRST_END_HOUR, response.FIRST_END_MIN),
                SecondPassBegin = ParseFromString(response.SECOND_BEGIN_DATE, response.SECOND_BEGIN_HOUR, response.SECOND_BEGIN_MIN),
                SecondPassEnd = ParseFromString(response.SECOND_END_DATE, response.SECOND_END_HOUR, response.SECOND_END_MIN)
            };

            DateTime ParseFromString(string date, string hour, string min)
            {
                if (string.IsNullOrWhiteSpace(date)) return new DateTime();

                var result = DateTime.Parse(date);

                if (!string.IsNullOrEmpty(hour) && double.TryParse(hour, out var h)) result += TimeSpan.FromHours(h);
                if (!string.IsNullOrEmpty(min) && double.TryParse(min, out var m)) result += TimeSpan.FromMinutes(m);

                return result;
            }
        }

        public async Task<string> SearchCatelogAsync(CourseId course, Term term)
        {
            var responseType = new
            {
                CATALOG_DATA = string.Empty
            };

            var response = await RequestGet(WebRegApi.SearchCatelog, responseType, new NameValueCollection
            {
                ["subjcode"] = course.Subject,
                ["crsecode"] = course.Code,
                ["termcode"] = term.Code,
            });

            return response.CATALOG_DATA;
        }

        public async Task<IReadOnlyList<string>> SearchRestrictionAsync(CourseId course, Term term)
        {
            var responseType = new[]
            {
                new
                {
                    CRSE_REGIS_TYPE_CD = string.Empty,
                    CRSE_REGIS_FLAG = string.Empty,
                    CRSE_REGIS_CODE = string.Empty
                }
            };

            var response = await RequestGet(WebRegApi.SearchRestriction, responseType, new NameValueCollection
            {
                ["subjcode"] = course.Subject,
                ["crsecode"] = course.Code,
                ["termcode"] = term.Code,
            });

            return response.Select(x => x.CRSE_REGIS_CODE).ToArray();
        }

        public async Task SearchSectionTextAsync(IEnumerable<int> sections, Term term)
        {
            var response = await RequestGetString(WebRegApi.SearchSectionText, new NameValueCollection
            {
                ["sectnumlist"] = string.Join(":", sections),
                ["termcode"] = term.Code,
            });

            // unknown response
            Console.WriteLine(response);
        }

        public async Task<IReadOnlyList<(string text, string courseId)>> SearchCourseTextAsync(Subject subject, Term term)
        {
            var responseType = new[]
            {
                new
                {
                    TEXT = string.Empty,
                    SUBJCRSE = string.Empty
                }
            };

            var response = await RequestGet(WebRegApi.SearchCourseText, responseType, new NameValueCollection
            {
                ["subjlist"] = subject.Code,
                ["termcode"] = term.Code,
            });

            return response.Select(x => (text: x.TEXT, courseId: x.SUBJCRSE)).ToArray();
        }

        public async Task<SectionList> SearchGroupDataAsync(CourseId course, Term term)
        {
            var response = await RequestGet<SectionList>(WebRegApi.SearchGroupData, new NameValueCollection
            {
                ["subjcode"] = course.Subject,
                ["crsecode"] = course.Code,
                ["termcode"] = term.Code
            });

            return response;
        }

        private async Task SendWrLoggerAsync()
        {
            var task = _client.PostAsync(WebRegApi.WrLogger, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["action"] = "START+PAGE",
                ["crsecode"] = "N/A",
                ["subjcode"] = "N/A",
                ["termcode"] = "N/A",
                ["result"] = "",
                ["sectnum"] = ""
            }));

            using (var msg = await task)
            {
                msg.EnsureSuccessStatusCode();
            }
        }

        public async Task GetStatusStartAsync(Term term)
        {
            var requestType = new[]
            {
                new
                {
                    ACADEMIC_LEVEL = string.Empty,
                    REGIS_STATUS = string.Empty,
                    ACADEMIC_STATUS = string.Empty,
                    TERM_SEQ_ID = 0
                }
            };

            await RequestGet(WebRegApi.GetStatusStart, requestType, new NameValueCollection
            {
                ["seqid"] = term.SequenceId.ToString(),
                ["termcode"] = term.Code,
            });
        }

        private static class WebRegApi
        {
            // no verification needed
            public const string GetTerm = "/webreg2/svc/wradapter/get-term";
            public const string WrLogger = "/webreg2/svc/wradapter/wr-logger";
            public const string GetStatusStart = "/webreg2/svc/wradapter/get-status-start";
            public const string GetMsgToProceed = "/webreg2/svc/wradapter/get-msg-to-proceed";

            // core functions
            public const string GetPrerequisites = "/webreg2/svc/wradapter/secure/get-prerequisites";
            public const string GetClass = "/webreg2/svc/wradapter/secure/get-class";
            public const string AddEnroll = "/webreg2/svc/wradapter/secure/add-enroll";
            public const string DropEnroll = "/webreg2/svc/wradapter/secure/drop-enroll";
            public const string EditEnroll = "/webreg2/svc/wradapter/secure/edit-enroll";
            public const string AddWait = "/webreg2/svc/wradapter/secure/add-wait";
            public const string DropWait = "/webreg2/svc/wradapter/secure/drop-wait";
            public const string EditWait = "/webreg2/svc/wradapter/secure/edit-wait";
            public const string SendEmail = "/webreg2/svc/wradapter/secure/send-email";
            public const string GetEnrollAddDate = "/webreg2/svc/wradapter/secure/get-enroll-add-dates";
            public const string GetMessagePass = "/webreg2/svc/wradapter/secure/get-msg-pass";
            public const string CheckEligibility = "/webreg2/svc/wradapter/check-eligibility";
            public const string LoadSubject = "/webreg2/svc/wradapter/secure/search-load-subject";
            public const string LoadCourses = "/webreg2/svc/wradapter/secure/search-get-crse-list";
            public const string GetPreAuth = "/webreg2/svc/wradapter/secure/get-preauth-info";

            public const string SearchCatelog = "/webreg2/svc/wradapter/secure/search-get-catalog";
            public const string SearchRestriction = "/webreg2/svc/wradapter/secure/search-get-restriction";
            public const string SearchSectionText = "/webreg2/svc/wradapter/secure/search-get-section-text";
            public const string SearchCourseText = "/webreg2/svc/wradapter/secure/search-get-crse-text";

            public const string SearchGroupData = "/webreg2/svc/wradapter/secure/search-load-group-data";
        }

        private async Task<T> RequestPost<T>(string api, T anonymousTypeObject, IEnumerable<KeyValuePair<string, string>> content)
        {
            using (var message = await _client.PostAsync(api, new FormUrlEncodedContent(content)))
            {
                ValidateResponse(message);

                var response = await message.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeAnonymousType(response, anonymousTypeObject);
            }
        }

        private async Task<string> RequestPostString(string api, IEnumerable<KeyValuePair<string, string>> content)
        {
            using (var message = await _client.PostAsync(api, new FormUrlEncodedContent(content)))
            {
                ValidateResponse(message);

                var response = await message.Content.ReadAsStringAsync();

                return response;
            }
        }

        private async Task<T> RequestGet<T>(string api, T anonymousTypeObject, NameValueCollection query)
        {
            return JsonConvert.DeserializeAnonymousType(await RequestGetString(api, query), anonymousTypeObject);
        }

        private async Task<T> RequestGet<T>(string api, NameValueCollection query)
        {
            return JsonConvert.DeserializeObject<T>(await RequestGetString(api, query));
        }

        private async Task<string> RequestGetString(string api, NameValueCollection queryCollection)
        {
            var queryString = queryCollection.BuildQueryString();

            using (var message = await _client.GetAsync(api + queryString))
            {
                ValidateResponse(message);

                var response = await message.Content.ReadAsStringAsync();

                return response;
            }
        }

        private static void ValidateResponse(HttpResponseMessage message)
        {
            switch (message.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    throw new NotSupportedException();

                case HttpStatusCode.InternalServerError:
                    Console.WriteLine(message.Content.ReadAsStringAsync().Result);
                    if (Debugger.IsAttached)
                        Debugger.Break();
                    break;

                case HttpStatusCode.OK:
                    if (string.Equals(message.Content.Headers.ContentType.MediaType, MediaTypeHtml, StringComparison.Ordinal))
                    {
                        Console.WriteLine(message.Content.ReadAsStringAsync().Result);

                        throw new Exception("Session timeout");
                    }
                    else if (!string.Equals(message.Content.Headers.ContentType.MediaType, MediaTypeJson,
                        StringComparison.Ordinal))
                    {
                        throw new NotSupportedException();
                    }

                    break;
            }
        }

        private const string MediaTypeJson = "application/json";
        private const string MediaTypeHtml = "text/html";
    }
}
