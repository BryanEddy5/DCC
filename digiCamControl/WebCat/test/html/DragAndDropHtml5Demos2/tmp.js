var messages = document.getElementById('messages');
var imageHolder = document.getElementById('imageHolder');
var holder = document.getElementById('holder'),
    tests = {
	filereader: typeof FileReader != 'undefined',
	dnd: 'draggable' in document.createElement('span'),
	formdata: !!window.FormData,
	progress: "upload" in new XMLHttpRequest
    }, 
    support = {
	filereader: document.getElementById('filereader'),
	formdata: document.getElementById('formdata'),
	progress: document.getElementById('progress')
    },
    acceptedTypes = {
	'image/png': true,
	'image/jpeg': true,
	'image/gif': true
    },
    progress = document.getElementById('uploadprogress'),
    fileupload = document.getElementById('upload');

"filereader formdata progress".split(' ').forEach(function (api) {
    if (tests[api] === false) {
	support[api].className = 'fail';
    } else {
	// FFS. I could have done el.hidden = true, but IE doesn't support
	// hidden, so I tried to create a polyfill that would extend the
	// Element.prototype, but then IE10 doesn't even give me access
	// to the Element object. Brilliant.
	support[api].className = 'hidden';
    }
});

function previewfile(file) {
    if (tests.filereader === true && acceptedTypes[file.type] === true) {
	var reader = new FileReader();
	reader.onload = function (event) {
	    var image = new Image();
	    image.src = event.target.result;
	    console.log("file", file);
	    console.log("image", image);
	    image.width = 200; // a fake resize
	    // imageHolder.appendChild(image);
	    var alias = extractAlias(file.name);
	    var elem = fileLayout(file, image);
	    imageHolder.insertBefore(elem, imageHolder.childNodes[0]);
	};

	reader.readAsDataURL(file);
    }  else {
	messages.innerHTML += '<p>Uploaded ' + file.name + ' ' + (file.size ? (file.size/1024|0) + 'K' : '');
	console.log(file);
    }
}

function extractAlias(filename) {
    var alias = null;
    var matches = filename.match(/^[a-zA-Z]+/);
    if (matches) {
        alias = matches[0];
    }
    
    return alias;
}

// https://jsfiddle.net/xg7tek9j/7/
function generateUUID() {
    var d = new Date().getTime();
    if(window.performance && typeof window.performance.now === "function"){
        d += performance.now();; //use high-precision timer if available
    }
    var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        var r = (d + Math.random()*16)%16 | 0;
        d = Math.floor(d/16);
        return (c=='x' ? r : (r&0x3|0x8)).toString(16);
    });
    return uuid;
}

function fileLayout(file, image) {
    var div = document.createElement("div");
    image.className = "thumbLeft";
    div.appendChild(image);
    var table = document.createElement("table");
    table.appendChild(attrRow("Name", file.name));
    table.appendChild(attrRow("Size", file.size));
    table.appendChild(attrRow("Type", file.type));
    table.appendChild(attrRow("LastModified", file.lastModifiedDate));
    div.appendChild(table);

    var last = document.createElement("p");
    last.className = "thumbClear";
    div.appendChild(last);

    return div;
}

function attrRow(attrName, attrValue) {
    var row = document.createElement("tr");
    var attrItem = document.createElement("td");
    var valueItem = document.createElement("td");
    var attrText = document.createTextNode(attrName);
    var sepText = document.createTextNode(":");
    var valueText = document.createTextNode(attrValue);
    attrItem.className = "attrName";
    attrItem.appendChild(attrText);
    attrItem.appendChild(sepText);
    valueItem.appendChild(valueText);
    row.appendChild(attrItem);
    row.appendChild(valueItem);

    return row;
}    

function readfiles(files) {
    // debugger;
    var formData = tests.formdata ? new FormData() : null;
    for (var i = 0; i < files.length; i++) {
	if (tests.formdata) formData.append('file', files[i]);
	previewfile(files[i]);
    }

    // now post a new XHR request
    if (tests.formdata) {
	var xhr = new XMLHttpRequest();
	xhr.open('POST', '/devnull.php');
	xhr.onload = function() {
	    progress.value = progress.innerHTML = 100;
	};

	if (tests.progress) {
	    xhr.upload.onprogress = function (event) {
		if (event.lengthComputable) {
		    var complete = (event.loaded / event.total * 100 | 0);
		    progress.value = progress.innerHTML = complete;
		}
	    }
	}

	xhr.send(formData);
    }
}

if (tests.dnd) { 
    var dragBoxEnter = function () { this.className = 'hover'; return false; };
    var dragBoxLeave = function () { this.className = ''; return false; };

    holder.ondragover = dragBoxEnter;
    holder.ondragenter = dragBoxEnter;

    holder.ondragend = dragBoxLeave;
    holder.ondragleave = dragBoxLeave;

    holder.ondrop = function (e) {
	this.className = '';
	e.preventDefault();
	readfiles(e.dataTransfer.files);
    }
} else {
    fileupload.className = 'hidden';
    fileupload.querySelector('input').onchange = function () {
	readfiles(this.files);
    };
}
