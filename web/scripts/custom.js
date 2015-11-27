// Stores the initalized CodeMirror editor objects (we need to refresh
// them when switching tabs, otherwise they do not load properly)
var allEditors = [];

// Stores information about the tests as a collection of records:
// { id:"[ID]", section:"[SECTION]", source:"[SOURCE]" }
var kataTests = [];


// ---------------------------------------------------------------------
// Switching tabs and other UI functions
// ---------------------------------------------------------------------

function showTab(id)
{
  $(".tabs-wrapper a").removeClass("selected");
  $("#link_ft_" + id).addClass("selected");
  $("#link_hd_" + id).addClass("selected");
  
  $("#main .tab").removeClass("selected");
  $("#" + id).addClass("selected");
  
  allEditors.forEach(function(ed) { 
    ed.setValue(ed.getValue()); 
  });
}

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
      counter++; 
      if (counter >= 19) counter = 0;
      setTimeout(spin, 100);
    }
  };
  setTimeout(spin, 500);
  return function () { finished = true; };
}


// ---------------------------------------------------------------------
// Evaluation of katas
// ---------------------------------------------------------------------

function updateAlertBox(id, anyFailed)
{
  $("#" + id + "_alert_info").hide();
  var f = $("#" + id + "_alert_fail").stop();
  var s = $("#" + id + "_alert_success").stop();
  if (anyFailed) f.fadeIn(100); else f.hide();
  if (!anyFailed) s.fadeIn(100); else s.hide();
  window[id + "_animation"] = setTimeout(function() {
    f.fadeOut(1000);
    s.fadeOut(1000);
  }, 5000);
}

function formatSource(test, source)
{
  var testSource = test.source.match(/[^\r\n]+/g).map(function(s){ return "  " + s; }).join("\n");
  return source + "\nlet test() =\n" + testSource;
}

function updateTestResult(test, newClass)
{
  var resEl = $("#" + test.id + " .test-result");
  resEl.removeClass("test-fail");
  resEl.removeClass("test-success");
  resEl.removeClass("test-unknown");
  resEl.addClass("test-" + newClass);
}

function evalKata(section, id)
{
  // Start the spinner & init alert box
  var stop = startSpinning($("#" + id + "_spinner").get()[0]);
  if (window[id + "_animation"]) clearTimeout(window[id + "_animation"]);
  
  // Get the attempted solution and relevant tests
  var source = window[id + '_cm_editor'].getValue();  
  var tests = kataTests.filter(function(test) { 
    return test.section == section; });

  // Convert test source to JavaScript and evaluate them
  var remaining = tests.length;
  var anyFailed = false;
  tests.forEach(function(test) {
    $.ajax({
      url: "/run", data: formatSource(test, source), 
      contentType: "text/fsharp", type: "POST", dataType: "text"
    }).done(function (data) {    
      
      // Evaluate the test & show the test result
      var res = eval("(function(){ " + data + "; return test(); })()");
      var newClass = res ? "success" : "fail";      
      updateTestResult(test, newClass);

      // When we run all tess, display final result
      anyFailed = anyFailed || !res;      
      remaining--;      
      if (remaining == 0) {
        updateAlertBox(id, anyFailed);        
        stop();
      }
    });
  });
}

// ---------------------------------------------------------------------
// Setting up the CodeMirror editor
// ---------------------------------------------------------------------

function setupEditorHelper(id) {
  var source = window[id + "_source"];
  var element = document.getElementById(id + "_editor");

  // Setup the CodeMirror editor with fsharp mode
  var editor = CodeMirror(element, {
    value: source, mode: 'fsharp', lineNumbers: false
  });

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

function setupEditor(id)
{
  var edEl = $("#" + id + "_editor");
  edEl.show();
  if (window[id + "_cm_editor"] == null)
  {
    var ed = setupEditorHelper(id);
    window[id + "_cm_editor"] = ed;
    allEditors.push(ed);    
  }
}

