namespace Ciel.Birb;

public struct Status(ushort code, string message)
{
    public static readonly Status Continue = new(100, "Continue");
    public static readonly Status SwitchingProtocols = new(101, "Switching Protocols");
    public static readonly Status Processing = new(102, "Processing");
    public static readonly Status EarlyHints = new(103, "Early Hints");

    public static readonly Status OK = new(200, "OK");
    public static readonly Status Created = new(201, "Created");
    public static readonly Status Accepted = new(202, "Accepted");
    public static readonly Status NonAuthoritativeInfo = new(203, "Non-Authoritative Information");
    public static readonly Status NoContent = new(204, "No Content");
    public static readonly Status ResetContent = new(205, "Reset Content");
    public static readonly Status PartialContent = new(206, "Partial Content");
    public static readonly Status MultiStatus = new(207, "Multi-Status");
    public static readonly Status AlreadyReported = new(208, "Already Reported");
    public static readonly Status IMUsed = new(226, "IM Used");

    public static readonly Status MultipleChoices = new(300, "Multiple Choices");
    public static readonly Status MovedPermanently = new(301, "Moved Permanently");
    public static readonly Status Found = new(302, "Found");
    public static readonly Status SeeOther = new(303, "See Other");
    public static readonly Status NotModified = new(304, "Not Modified");
    public static readonly Status UseProxy = new(305, "Use Proxy");
    public static readonly Status TemporaryRedirect = new(307, "Temporary Redirect");
    public static readonly Status PermanentRedirect = new(308, "Permanent Redirect");

    public static readonly Status BadRequest = new(400, "Bad Request");
    public static readonly Status Unauthorized = new(401, "Unauthorized");
    public static readonly Status PaymentRequired = new(402, "Payment Required");
    public static readonly Status Forbidden = new(403, "Forbidden");
    public static readonly Status NotFound = new(404, "Not Found");
    public static readonly Status MethodNotAllowed = new(405, "Method Not Allowed");
    public static readonly Status NotAcceptable = new(406, "Not Acceptable");
    public static readonly Status ProxyAuthRequired = new(407, "Proxy Authentication Required");
    public static readonly Status RequestTimeout = new(408, "Request Timeout");
    public static readonly Status Conflict = new(409, "Conflict");
    public static readonly Status Gone = new(410, "Gone");
    public static readonly Status LengthRequired = new(411, "Length Required");
    public static readonly Status PreconditionFailed = new(412, "Precondition Failed");
    public static readonly Status PayloadTooLarge = new(413, "Payload Too Large");
    public static readonly Status URITooLong = new(414, "URI Too Long");
    public static readonly Status UnsupportedMediaType = new(415, "Unsupported Media Type");
    public static readonly Status RangeNotSatisfiable = new(416, "Range Not Satisfiable");
    public static readonly Status ExpectationFailed = new(417, "Expectation Failed");
    public static readonly Status ImATeapot = new(418, "I'm a teapot");
    public static readonly Status MisdirectedRequest = new(421, "Misdirected Request");
    public static readonly Status UnprocessableContent = new(422, "Unprocessable Content");
    public static readonly Status Locked = new(423, "Locked");
    public static readonly Status FailedDependency = new(424, "Failed Dependency");
    public static readonly Status TooEarly = new(425, "Too Early");
    public static readonly Status UpgradeRequired = new(426, "Upgrade Required");
    public static readonly Status PreconditionRequired = new(428, "Precondition Required");
    public static readonly Status TooManyRequests = new(429, "Too Many Requests");
    public static readonly Status RequestHeaderFieldsTooLarge = new(431, "Request Header Fields Too Large");
    public static readonly Status UnavailableForLegalReasons = new(451, "Unavailable For Legal Reasons");

    public static readonly Status InternalServerError = new(500, "Internal Server Error");
    public static readonly Status NotImplemented = new(501, "Not Implemented");
    public static readonly Status BadGateway = new(502, "Bad Gateway");
    public static readonly Status ServiceUnavailable = new(503, "Service Unavailable");
    public static readonly Status GatewayTimeout = new(504, "Gateway Timeout");
    public static readonly Status HTTPVersionNotSupported = new(505, "HTTP Version Not Supported");
    public static readonly Status VariantAlsoNegotiates = new(506, "Variant Also Negotiates");
    public static readonly Status InsufficientStorage = new(507, "Insufficient Storage");
    public static readonly Status LoopDetected = new(508, "Loop Detected");
    public static readonly Status NotExtended = new(510, "Not Extended");
    public static readonly Status NetworkAuthenticationRequired = new(511, "Network Authentication Required");

    public ushort Code => code;
    public string Message => message;
}