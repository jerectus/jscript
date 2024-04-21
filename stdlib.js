var io = new ActiveXObject("Scripting.FileSystemObject");
var shell = new ActiveXObject("WScript.Shell");
var ios = {
    forEach: function (list, fn) {
        var i = 0;
        for (var e = new Enumerator(list); !e.atEnd(); e.moveNext(), i++) {
            fn(e.item(), i, list);
        }
    },
    open: function (path, mode, charset) {
        var s = new ActiveXObject("ADODB.Stream");
        s.type = 2/*Text*/;
        s.charset = charset || "UTF-8";
        s.open();
        if (mode == "r") {
            s.loadFromFile(path);
        }
        return {
            close: function () {
                if (mode == "w" || mode == "a") {
                    if (s.charset == "UTF-8") {
                        s.position = 0;
                        s.type = 1/*Binary*/;
                        s.position = 3;
                        var bin = s.read();
                        s.close();
                        s.open();
                        s.position = 0;
                        s.type = 1/*Binary*/;
                        s.write(bin);
                    }
                    if (mode == "w") {
                        s.saveToFile(path, 2);
                    } else {
                        if (!io.fileExists(path)) {
                            s.saveToFile(path, 2);
                        } else {
                            var append = io.getTempName();
                            var original = io.getTempName();
                            s.saveToFile(append, 2);
                            io.moveFile(path, original)
                            var cmd = "cmd /c copy " + original + "+" + append + " " + path;
                            shell.run(cmd, 0, true);
                            io.deleteFile(original);
                            io.deleteFile(append);
                        }
                    }
                }
                s.close();
            },
            atEndOfStream: function () {
                return s.EOS;
            },
            readLine: function () {
                return s.readText(-2/*ReadLine*/);
            },
            readAll: function () {
                return s.readText(-1/*ReadAll*/);
            },
            writeLine: function (line) {
                return s.writeText(line, 1/*WithNewLine*/);
            },
            write: function (content) {
                return s.writeText(content, 0/*WithoutNewLine*/);
            }
        };
    },
    load: function (path) {
        var s = ios.open(path, "r", "UTF-8");
        var content = s.readAll();
        s.close();
        return content;
    },
    save: function (path, content, append) {
        var s = ios.open(path, append === true ? "a" : "w", "UTF-8");
        s.write(content);
        s.close();
    }
};
