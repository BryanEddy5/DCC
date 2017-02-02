function PanAndZoom(canvasId, canvasImage, zoomSliderId, zoomPercentId) {
    console.log("hello from panAndZoom js");

    // regular element since JQuery doesn't cover canvas
    var canvas = $("#" + canvasId)[0];

    var lastX = canvas.width / 2;
    var lastY = canvas.height / 2;
    var dragStart;
    var dragged;

    var croppedCanvas;
    var croppedImage;

    var zoomFactorStep = 0.01;
    var minZoom = 1.0;
    var maxZoom = 3.0;

    var zoomSlider;
    if (zoomSliderId) {
	zoomSlider = $("#" + zoomSliderId);
    }
    if (zoomSlider) {
        zoomFactorStep = zoomSlider.attr("step");
        minZoom = zoomSlider.attr("min");
        maxZoom = zoomSlider.attr("max");
    }
    var zoomPercentBox;
    if (zoomPercentId) {
	zoomPercentBox = $("#" + zoomPercentId);
    }

    var ctx = canvas.getContext('2d');
    trackTransforms(ctx, updateZoomSlider);

    redraw();
    updateZoomPercentDisplay(1.0);

    this.resetCrop = function() {
        ctx.setTransform(1, 0, 0, 1, 0, 0);
        redraw();
    };

    this.getCropCorners = function() {
        var pt1 = ctx.transformedPoint(0,0);
        var pt2 = ctx.transformedPoint(canvas.width,canvas.height);

        return [pt1, pt2];
    };

    this.showCroppedView = function(croppedViewId) {
        // regular element since JQuery doesn't cover canvas
        croppedCanvas = $("#" + croppedViewId)[0];
        croppedImage = new Image;
        croppedImage.src = canvasImage.src;
        redrawCropped();
    };

    var warningCallback;
    this.addWarningCallback = function(warningCallbackFunction) {
        warningCallback = warningCallbackFunction;
    };

    // the following callbacks are for diagnostic purposes
    var xformDisplayCallback;
    this.addXformDisplayCallback = function(xformDisplayCallbackFunction) {
        xformDisplayCallback = xformDisplayCallbackFunction;
        xformDisplay();
    }

    var mappedCornersDisplayCallback;
    this.addMappedCornersDisplayCallback = function(mappedCornersDisplayCallbackFunction) {
        mappedCornersDisplayCallback = mappedCornersDisplayCallbackFunction;
        mappedCornersDisplay();
    }

    var unmappedCornersDisplayCallback;
    this.addUnmappedCornersDisplayCallback = function(unmappedCornersDisplayCallbackFunction) {
        unmappedCornersDisplayCallback = unmappedCornersDisplayCallbackFunction;
        unmappedCornersDisplay();
    }

    var mouseDisplayCallback;
    this.addMouseDisplayCallback = function(mouseDisplayCallbackFunction) {
        mouseDisplayCallback = mouseDisplayCallbackFunction;
    }

    // IE needs "change"
    if (zoomSlider) {
        zoomSlider.on("input change", function(event) {
            warningMessage("");
            updateZoomPercentDisplay();
            var scaleFactor = zoomSlider.val();
            var centerPoint = ctx.transformedPoint(canvas.width / 2, canvas.height / 2);
            lastX = centerPoint.x;
            lastY = centerPoint.y;
            zoomAroundPoint(scaleFactor);
            redraw();
        });
    }

    function xformDisplay() {
        if (xformDisplayCallback) {
            var xf = ctx.getTransform();
            xformDisplayCallback(xf);
        }
    }

    function mappedCornersDisplay() {
        if (mappedCornersDisplayCallback) {
            var pt1 = ctx.transformedPoint(0,0);
            var pt2 = ctx.transformedPoint(canvas.width,canvas.height);
            mappedCornersDisplayCallback(pt1, pt2);
        }
    }

    function unmappedCornersDisplay() {
        if (mappedCornersDisplayCallback) {
            var pt1 = ctx.untransformedPoint(0,0);
            var pt2 = ctx.untransformedPoint(canvas.width,canvas.height);
            unmappedCornersDisplayCallback(pt1, pt2);
        }
    }

    function mouseDisplay(mx, my) {
        if (mouseDisplayCallback) {
            var pt = {x: mx, y: my};
            var xfmPt = ctx.transformedPoint(mx, my);
            mouseDisplayCallback(pt, xfmPt);
        }
    }

    function redraw() {
        // Clear the entire canvas
        var p1 = ctx.transformedPoint(0,0);
        var p2 = ctx.transformedPoint(canvas.width,canvas.height);
        var xf = ctx.getTransform();
        ctx.clearRect(p1.x, p1.y, p2.x - p1.x, p2.y - p1.y);

        ctx.drawImage(canvasImage, 0, 0, canvas.width, canvas.height);

        // draw bounding box
        // 88 / 256 = 0.34375 for opacity
        ctx.strokeStyle = "rgba(214, 249, 249, 0.34375)"; // "blue";
        var lineWidth = 4;
        var p3 = ctx.transformedPoint(lineWidth, lineWidth);
        ctx.lineWidth = p3.x - p1.x;
	// The following are based on 280x420
        var bb1 = ctx.transformedPoint(42, 48);
        var bb2 = ctx.transformedPoint(234, 302);
        ctx.strokeRect(bb1.x, bb1.y, bb2.x - bb1.x, bb2.y - bb1.y);

        redrawCropped();

        xformDisplay();
        mappedCornersDisplay();
        unmappedCornersDisplay();
    }

    function redrawCropped() {
        if (croppedCanvas) {
            var croppedCtx = croppedCanvas.getContext('2d');
            var width = croppedCanvas.width;
            var height = croppedCanvas.height;

            // Clear the entire canvas
            croppedCtx.clearRect(0, 0, width, height);

            croppedCtx.drawImage(croppedImage, 0, 0, width, height);

            var pt1 = ctx.transformedPoint(0,0);
            var pt2 = ctx.transformedPoint(canvas.width,canvas.height);

            croppedCtx.strokeStyle = "green";
            croppedCtx.lineWidth = 1;
            croppedCtx.strokeRect(pt1.x, pt1.y, pt2.x - pt1.x, pt2.y - pt1.y);
        }
    }

    canvas.addEventListener('mousedown', function(evt) {
        warningMessage("");
        document.body.style.mozUserSelect = document.body.style.webkitUserSelect = document.body.style.userSelect = 'none';
        lastX = evt.offsetX || (evt.pageX - canvas.offsetLeft);
        lastY = evt.offsetY || (evt.pageY - canvas.offsetTop);
        dragStart = ctx.transformedPoint(lastX, lastY);
        dragged = false;
    }, false);

    canvas.addEventListener('mousemove', function(evt){
        lastX = evt.offsetX || (evt.pageX - canvas.offsetLeft);
        lastY = evt.offsetY || (evt.pageY - canvas.offsetTop);
        mouseDisplay(lastX, lastY);
        dragged = true;
        if (dragStart) {
            warningMessage("");
            var pt = ctx.transformedPoint(lastX,lastY);
            ctx.translate(pt.x-dragStart.x,pt.y-dragStart.y);
            redraw();
        }
    }, false);

    canvas.addEventListener('mouseup', function(evt){
        warningMessage("");
        if (!dragged) {
            zoomByClicks(evt.shiftKey ? -1 : 1 );
            redraw();
        }
        else {
            if (dragStart) {
                lastX1 = evt.offsetX || (evt.pageX - canvas.offsetLeft);
                lastY1 = evt.offsetY || (evt.pageY - canvas.offsetTop);

                var pt = ctx.transformedPoint(lastX, lastY);
                ctx.translate(pt.x - dragStart.x, pt.y - dragStart.y);

                adjustPanBoundaries();

                redraw();
            }
        }
        dragStart = null;
    },false);

    function handleScroll(evt){
        warningMessage("");
        var delta = evt.wheelDelta ? evt.wheelDelta / 40 : evt.detail ? -evt.detail : 0;
        if (delta) {
            zoomByClicks(delta);
            redraw();
        }
        return evt.preventDefault() && false;
    };

    canvas.addEventListener('DOMMouseScroll', handleScroll, false);
    canvas.addEventListener('mousewheel', handleScroll, false);

    function adjustPanBoundaries() {
        var c1c = ctx.untransformedPoint(0, 0);
        var c2c = ctx.untransformedPoint(canvas.width, canvas.height);
        var currWidth = c2c.x - c1c.x;
        var currHeight = c2c.y  - c1c.y;

        var xf = ctx.getTransform();

        var zoomFactors = [1];
        // may need to grow by the number of pixels exposed by pan
        if (c1c.x > 0) {
            zoomFactors.push(1 + (c1c.x / currWidth));
        }
        if (c1c.y > 0) {
            zoomFactors.push(1 + (c1c.y / currHeight));
        }
        if (c2c.x < canvas.width) {
            zoomFactors.push(1 + ((canvas.width - c2c.x) / currWidth));
        }
        if (c2c.y > canvas.height) {
            zoomFactors.push(1 + ((canvas.height - c2c.y) / currHeight));
        }

        var zoomFactor = Math.max.apply(null, zoomFactors);
        if (zoomFactor > 1) {
            zoomFactor = xf.a * zoomFactor;
            zoomFactor = limitZoom(zoomFactor);
            // zoomByFactor(zoomFactor);
            zoomAroundPoint(zoomFactor);
            xf = ctx.getTransform();

            adjustZoomBoundaries();
        }
    }

    function zoomByClicks(clicks){
        var xf = ctx.getTransform();
        var zoomFactor = xf.a + (zoomFactorStep * clicks);
        zoomAroundPoint(zoomFactor);
    }

    function zoomAroundPoint(zoomFactor) {

        zoomFactor = limitZoom(zoomFactor);

        var pt = ctx.transformedPoint(lastX, lastY);
        ctx.translate(pt.x,pt.y);

        zoomByFactor(zoomFactor);

        ctx.translate(-pt.x, -pt.y);

        adjustZoomBoundaries();
    }

    function limitZoom(zoomFactor) {
        if (zoomFactor > maxZoom) {
            warningMessage("Zoom limited by max zoom of " + maxZoom);
            zoomFactor = maxZoom;
        }
        else if (zoomFactor < minZoom) {
            warningMessage("Zoom limited by min zoom of " + minZoom);
            zoomFactor = minZoom;
        }

        return zoomFactor;
    }

    function adjustZoomBoundaries() {
        var xf = ctx.getTransform();
        var c1c = ctx.untransformedPoint(0, 0);
        var c2c = ctx.untransformedPoint(canvas.width, canvas.height);

        if (c1c.x > 0) {
            xf.e = 0;
        }
        if (c1c.y > 0) {
            xf.f = 0;
        }
        if (c2c.x < canvas.width) {
            xf.e += (canvas.width - c2c.x) * xf.a;
        }
        if (c2c.y < canvas.height) {
            xf.f += (canvas.height - c2c.y) * xf.d;
        }

        ctx.setTransform(xf.a, xf.b, xf.c, xf.d, xf.e, xf.f);
    }

    function zoomByFactor(zoomFactor) {
        var xf = ctx.getTransform();
        ctx.setTransform(zoomFactor, xf.b, xf.c, zoomFactor, xf.e, xf.f);
    }

    function updateZoomSlider(zoomFactor) {
        if (zoomSlider) {
            zoomSlider.val(zoomFactor);
        }
        updateZoomPercentDisplay(zoomFactor);
    }

    function updateZoomPercentDisplay(zoomFactor) {
        if (zoomPercentBox) {
            zoomPercentBox.val(Math.round(zoomFactor * 100) - 100);
        }
    }

    function warningMessage(message) {
        if (warningCallback) {
            warningCallback(message);
        }
        else {
            console.log(message);
        }
    }
};

// Adds ctx.getTransform() - returns an SVGMatrix
// Adds ctx.transformedPoint(x,y) - returns an SVGPoint
// Adds ctx.untransformedPoint(x,y) - returns canvas point
function trackTransforms(ctx, zoomUpdate){
    var svg = document.createElementNS("http://www.w3.org/2000/svg",'svg');
    var xform = svg.createSVGMatrix();
    var zoomUpdate = zoomUpdate;

    ctx.getTransform = function() {
        return xform;
    };

    var scale = ctx.scale;
    ctx.scale = function(sx, sy) {
        xform = xform.scaleNonUniform(sx, sy);
        return scale.call(ctx, sx, sy);
    };

    var translate = ctx.translate;
    ctx.translate = function(dx, dy) {
        xform = xform.translate(dx, dy);
        return translate.call(ctx, dx, dy);
    };

    var setTransform = ctx.setTransform;
    ctx.setTransform = function(a,b,c,d,e,f){
        xform.a = a;
        xform.b = b;
        xform.c = c;
        xform.d = d;
        xform.e = e;
        xform.f = f;
        zoomUpdate(a);
        return setTransform.call(ctx,a,b,c,d,e,f);
    };

    var pt  = svg.createSVGPoint();
    ctx.transformedPoint = function(x, y){
        pt.x = x; pt.y = y;
        return pt.matrixTransform(xform.inverse());
    }

    ctx.untransformedPoint = function(x, y) {
        var upt = {
            x: x * xform.a + y * xform.c + xform.e,
            y: x * xform.b + y * xform.d + xform.f
        };
        return upt;
    }
}
