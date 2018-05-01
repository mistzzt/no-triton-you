using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TritonKinshi.Core.Extensions;
using CourseIdList = System.Collections.Immutable.ImmutableList<TritonKinshi.Core.CourseId>;
using SubjectList = System.Collections.Immutable.ImmutableList<TritonKinshi.Core.Subject>;
using TermList = System.Collections.Immutable.ImmutableList<TritonKinshi.Core.Term>;

namespace TritonKinshi.Core
{
    public sealed class PhantomJsWebReg : IWebReg
    {
        private const string PhantomJsPath = "phantomjs.exe";
        private const string ScriptPath = "loadWebReg.js";
        private const string QuarterDefault = "S118";

        private const string PersistedCookiePath = "cookies.json";

        private readonly HttpClient _client;
        private readonly CookieContainer _container;

        private string _cookiesPath;
        private bool _disposed;

        internal PhantomJsWebReg(HttpClient client, CookieContainer container)
        {
            _client = client;
            _container = container;
        }

        internal static PhantomJsWebReg FromPersistedCredentials()
        {
            if (!File.Exists(PersistedCookiePath))
            {
                throw new FileNotFoundException("Cannot find credentials", PersistedCookiePath);
            }

            var container = new CookieContainer();

            var clientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = container
            };

            var client = new HttpClient(clientHandler)
            {
                BaseAddress = new Uri(Urls.ActUrl)
            };
            var instance = new PhantomJsWebReg(client, container) { _cookiesPath = PersistedCookiePath };
            instance.LoadCookies().Wait();

            return instance;
        }

        private sealed class LowerContractResolver : DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                return propertyName.ToLowerInvariant();
            }
        }

        private async Task WriteCookies(string path = null)
        {
            var list = new List<object>();

            foreach (Cookie cookie in _container.GetAllRelatedCookies())
            {
                list.Add(new
                {
                    name = cookie.Name,
                    value = cookie.Value,
                    domain = cookie.Domain,
                    path = cookie.Path,
                    httponly = cookie.HttpOnly
                });
            }

            _cookiesPath = path ?? Path.GetTempFileName();
            using (var stream = new FileStream(_cookiesPath, FileMode.Create))
            {
                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(JsonConvert.SerializeObject(list));
                }
            }
        }

        private Task StartPhantomJs()
        {
            return Task.Run(() =>
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = PhantomJsPath,
                    Arguments = string.Join(" ",
                        "--ignore-ssl-errors=true",
                        "--local-to-remote-url-access=true",
                        ScriptPath,
                        _cookiesPath,
                        QuarterDefault
                    ),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                if (process == null)
                {
                    return;
                }

                process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                process.BeginOutputReadLine();
                process.WaitForExit();
            });
        }

        internal async Task PrepareWebReg()
        {
            await WriteCookies();
            await StartPhantomJs();
            await LoadCookies();

            await WriteCookies(PersistedCookiePath);
        }

        private async Task LoadCookies()
        {
            using (var reader = new StreamReader(new FileStream(_cookiesPath, FileMode.Open)))
            {
                var content = await reader.ReadToEndAsync();
                var cookies = JsonConvert.DeserializeObject<List<Cookie>>(content, new JsonSerializerSettings
                {
                    ContractResolver = new LowerContractResolver()
                });

                foreach (var cookie in cookies)
                {
                    _container.Add(cookie);
                }
            }
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

        public async Task<TermList> GetTermsAsync()
        {
            var response = await _client.GetStringAsync(WebRegApi.GetTerm);
            Console.WriteLine(response);

            var terms = JsonConvert.DeserializeObject<Term[]>(response);

            return terms.ToImmutableList();
        }

        public async Task<bool> CheckEligibilityAsync(Term term)
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
                ["logged"] = false.ToString()
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

            return subjects.ToImmutableList();
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
            }).ToImmutableList();
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
                Code = x.CRSE_CODE.Trim()
            }).ToImmutableList();

            return list;
        }

        public async Task<CourseIdList> GetEnrolledCoursesAsync(Term term)
        {
            var responseType = new[]
            {
                new
                {
					// personal information
					PERSON_ID = string.Empty,
                    ENROLL_STATUS = string.Empty,
                    GRADE_OPTION = string.Empty,

					// course id
					SUBJ_CODE = string.Empty,
                    CRSE_CODE = string.Empty,
                    SECTION_NUMBER = 0,
                    SECT_CODE = string.Empty,

					// course information
					START_DATE = string.Empty,
                    CRSE_TITLE = string.Empty,
                    LONG_DESC = string.Empty,
                    PERSON_FULL_NAME = string.Empty,
                    TERM_CODE = string.Empty,

					// time and location
					DAY_CODE = string.Empty,
                    BEGIN_HH_TIME = 0,
                    END_HH_TIME = 0,
                    BEGIN_MM_TIME = 0,
                    END_MM_TIME = 0,
                    BLDG_CODE = string.Empty,
                    ROOM_CODE = string.Empty,
					
					// unknown parts
					SECT_CREDIT_HRS = 0d,
                    GRADE_OPTN_CD_PLUS = string.Empty,
                    WT_POS = string.Empty,
                    PRIMARY_INSTR_FLAG = string.Empty,
                    FK_PCH_INTRL_REFID = 0,
                    NEED_HEADROW = false,
                    FK_SPM_SPCL_MTG_CD = string.Empty,
                    SECT_CREDIT_HRS_PL = string.Empty,
                    SECTION_HEAD = 0,
                    FK_CDI_INSTR_TYPE = string.Empty,
                    FK_SEC_SCTN_NUM = 0
                }
            };

            var cs = await RequestGet(WebRegApi.GetClass, responseType, new NameValueCollection
            {
                ["termcode"] = term.Code,
                ["schedname"] = string.Empty,
                ["final"] = string.Empty,
                ["sectnum"] = string.Empty
            });

            // todo: select real vals
            return cs.Select(x => new CourseId
            {
                Subject = x.SUBJ_CODE.Trim(),
                Code = x.CRSE_CODE.Trim(),
                Section = x.SECTION_NUMBER
            }).ToImmutableList();
        }

        public async Task<ImmutableList<(int order, CourseId course)>> GetPrerequisitesAsync(Term term, CourseId course)
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
            })).ToImmutableList();
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

        private static class WebRegApi
        {
            // no verification needed
            public const string GetTerm = "/webreg2/svc/wradapter/get-term";

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

            // not implemented yet
            public const string SearchCatelog = "/webreg2/svc/wradapter/secure/search-get-catalog";
            public const string SearchRestriction = "/webreg2/svc/wradapter/secure/search-get-restriction";
            public const string SearchSectionText = "/webreg2/svc/wradapter/secure/search-get-section-text";
            public const string SearchCourseText = "/webreg2/svc/wradapter/secure/search-get-crse-text";
        }

        private async Task<T> RequestGet<T>(string api, T anonymousTypeObject, NameValueCollection query)
        {
            var queryString = query.BuildQueryString();

            var response = await _client.GetStringAsync(api + queryString);

            return JsonConvert.DeserializeAnonymousType(response, anonymousTypeObject);
        }

        private async Task<T> RequestPost<T>(string api, T anonymousTypeObject, IEnumerable<KeyValuePair<string, string>> content)
        {
            var message = await _client.PostAsync(api, new FormUrlEncodedContent(content));
            var response = await message.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeAnonymousType(response, anonymousTypeObject);
        }

        private async Task<T> RequestGet<T>(string api, NameValueCollection query)
        {
            var queryString = query.BuildQueryString();

            var response = await _client.GetStringAsync(api + queryString);

            return JsonConvert.DeserializeObject<T>(response);
        }
    }
}
