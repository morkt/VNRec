Visual Novel recommendations applet
===================================

Recommendations are based on user votes on [VNDB](http://vndb.org/) and made using several different algorithms.

Requires manual import of the votes dump into SQLite database (example provided in VNDBTest project). Raw vote dump is available [elsewhere](http://vndb.org/t5562). Besides, some algorithms require preliminary time-consuming data preparation (lines responsible for this are commented out in code). This project is intented for personal use, so there's no point for me spending time implementing it properly for everyone's convenience.

Based on original java code by [raistlin](http://www.reddit.com/r/visualnovels/comments/2cwet0/vn_recommendation_algorithms/). I assume it's a public domain.

.NET implementation by morkt (c) 2014

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to
deal in the Software without restriction, including without limitation the
rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
sell copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
IN THE SOFTWARE.
