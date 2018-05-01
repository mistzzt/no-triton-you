var RETRY_TIME = 5;
var DEFAULT_QUARTER = "S218";

var fs = require('fs');
var webpage = require('webpage');
var page = webpage.create();

var args = require('system').args;
var cookieSrc = args[1];
var quarter = args[2] || DEFAULT_QUARTER;

var url = "https://act.ucsd.edu/webreg2/main?p1=" + quarter + "&p2=UN&p3=true#tabs-0";

console.log('[DEBUG] Cookie file path:', cookieSrc);
console.log('[DEBUG] Url:', url);

// set configurations
phantom.cookiesEnabled = true;
phantom.javascriptEnabled = true;
page.settings.javascriptEnabled = true;
page.settings.loadImages = false;
page.customHeaders = {
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8",
    "Accept-Language": "en,zh-CN;q=0.9,zh;q=0.8,zh-TW;q=0.7",
    "Cache-Control": "max-age=0",
    "Referer": url,
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.181 Safari/537.36",
    "Upgrade-Insecure-Requests": 1,
    "Origin": "https://act.ucsd.edu",
    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8"
};

// load cookies
if (fs.isFile(cookieSrc)) {
    Array.prototype.forEach.call(JSON.parse(fs.read(cookieSrc)), function (x) {
        phantom.addCookie(x);
    });
} else {
    console.log('[WARN] Cannot find source cookie file');
    phantom.exit(1);
}

// retry several times to ensure get correct result
var count = 0;
page.onLoadFinished = function (status) {
    console.log('[INFO] Page loaded with status', status);

    if (++count > RETRY_TIME) {
        fs.write(cookieSrc, JSON.stringify(phantom.cookies), "w");

        console.log('[INFO] File written.');
        phantom.exit(0);
    } else if (status !== 'success') {
        page.reload();
        console.log('[INFO] Retrying...');
    }
};

page.open(url, function (status) {
    console.log('[INFO] Page loaded!');
});