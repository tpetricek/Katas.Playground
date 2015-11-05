/**************************************** Top panel and Spinner stuff ****************************************/

function startSpinning(el)
{
  var counter = 0;
  var finished = false;
  function spin() {
    if (finished) {
      el.style.display = "none";
      return;
    } else {
      el.style.display = "block";
      var offset = counter * -21;
      el.style.backgroundPosition = "0px " + offset + "px";
      counter++; if (counter >= 19) counter = 0;
      setTimeout(spin, 100);
    }
  };
  setTimeout(spin, 500);
  return function () { finished = true; };
}

/**************************************** Common for editors/visualizers ****************************************/

function setSource(id, source, byHand)
{
  window[id + "_source"] = source;
  window[id + "_change"].forEach(function (f) { f(byHand); });
}

/**************************************** Setting up the editor ****************************************/

var kataTests = [];

function evalKata(source)
{
  kataTests.forEach(function(kata) {
    var kataSource = source + "\n" + kata.source;
    $.ajax({
      url: "/run", data: kataSource, contentType: "text/fsharp",
      type: "POST", dataType: "text"
    }).done(function (data) {
      var res = eval("(function(){ " + data + " })()");
      document.getElementById(kata.id + "_testresult").innerHTML = res;      
    });
  });
}

function setupEditor(id) {
  var source = window[id + "_source"];
  var element = document.getElementById(id + "_editor");

  // Setup the CodeMirror editor with fsharp mode
  var editor = CodeMirror(element, {
    value: source, mode: 'fsharp', lineNumbers: false
  });
  editor.focus();

  // Helper to send request to our server
  function request(operation, line, col) {
    var url = "/" + operation;
    if (line != null && col != null) url += "?line=" + (line + 1) + "&col=" + col;
    return $.ajax({
      url: url, data: editor.getValue(),
      contentType: "text/fsharp", type: "POST", dataType: "JSON"
    });
  }

  // Request type-checking and parsing errors from the server
  editor.compiler = new Compiler(editor, function () {
    request("check").done(function (data) {
      editor.compiler.updateMarkers(data.errors);
    });
  });

  // Request declarations & method overloads from the server
  editor.intellisense = new Intellisense(editor,
    function (position) {
      request("declarations", position.lineIndex, position.columnIndex).done(function (data) {
        editor.intellisense.setDeclarations(data.declarations);
      });
    },
    function (position) {
      request("methods", position.lineIndex, position.columnIndex).done(function (data) {
        editor.intellisense.setMethods(data.methods);
      });
    }
  );
  $(element).addClass("editor-cm-visible");
  return editor;
}

function switchEditor(id)
{
  var edEl = $("#" + id + "_editor");
  edEl.show();
  if (window[id + "_cm_editor"] == null)
    window[id + "_cm_editor"] = setupEditor(id);
}

