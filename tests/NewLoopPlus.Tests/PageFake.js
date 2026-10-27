// A fake root page for page.js: elements that keep their children, no game iframes, and a fake Exiled's Toolkit that
// records the panels handed to it.
var window = this;

function setTimeout() { return 1; }
function clearTimeout() {}
function setInterval() { return 1; }

class FakeEl {
  constructor(tag) {
    this.tagName = String(tag).toUpperCase();
    this.children = [];
    this.parentNode = null;
    this.style = {};
    this.id = '';
    this.textContent = '';
  }

  appendChild(child) {
    child.parentNode = this;
    this.children.push(child);

    return child;
  }

  remove() {
    var parent = this.parentNode;

    if (!parent) return;

    parent.children.splice(parent.children.indexOf(this), 1);
    this.parentNode = null;
  }
}

function findById(node, id) {
  if (node.id === id) return node;

  for (var i = 0; i < node.children.length; i++) {
    var found = findById(node.children[i], id);

    if (found) return found;
  }

  return null;
}

var document = {
  documentElement: new FakeEl('html'),
  head: new FakeEl('head'),
  body: new FakeEl('body'),
  createElement: function (tag) { return new FakeEl(tag); },
  getElementById: function (id) { return findById(this.head, id) || findById(this.body, id); },
  querySelectorAll: function () { return []; }
};

var handed = [];
window.ExiledUi = { size: function (el, options) { handed.push({ el: el, options: options }); } };

function toastNode() {
  return document.getElementById('slnlp-toast');
}
