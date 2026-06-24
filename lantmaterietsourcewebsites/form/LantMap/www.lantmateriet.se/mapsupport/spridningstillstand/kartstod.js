var featuresJSON;
var resultGeoJSON = "";
var testsUrl = window.location.pathname.substring(0, window.location.pathname.lastIndexOf('/')) + '/';
var editorResources;
var drawingsNum = 0;

var getGraphics;
var getGeoJSON;
var updateResultGeoJSON;

require([
    "esri/Map",
    "esri/geometry/Extent",
    "esri/layers/WMSLayer",
    "esri/views/MapView",
    "esri/widgets/ScaleBar",
    "esri/widgets/Search",
    "esri/geometry/geometryEngine",
    "esri/Graphic",
    "esri/layers/GraphicsLayer",
    "esri/geometry/Polygon",
    "esri/widgets/Home",
    "esri/widgets/Zoom",
    "esri/widgets/Expand",
    "esri/intl",
    "esri/widgets/LayerList",
    "esri/widgets/Sketch",
    "esri/widgets/Sketch/SketchViewModel",
    "esri/core/lang",
    "esri/geometry/projection",
    "esri/geometry/SpatialReference",
    "esri/geometry/Point",
    "esri/geometry/Polyline",
    "esri/layers/support/TileInfo",
    "esri/Viewpoint",
], function (
    ArcGISMap,
    Extent,
    WMSLayer,
    MapView,
    ScaleBar,
    Search,
    geometryEngine,
    Graphic,
    GraphicsLayer,
    Polygon,
    Home,
    Zoom,
    Expand,
    intl,
    LayerList,
    Sketch,
    SketchViewModel,
    esriLang,
    projection,
    SpatialReference,
    Point,
    Polyline,
    TileInfo,
    Viewpoint,
) {
    async function IntlLoad() {
        // configure translation
        const loader = intl.createJSONLoader({
            pattern: "my-application/",
            base: "my-application",
            location: "translations/editorResources"
        });
        intl.registerMessageBundleLoader(loader);

        return await intl.fetchMessageBundle("my-application/editorResources");
    }
    IntlLoad().then(function (value) {
        editorResources = value;
        main();
    });

    // if url rewrite is set up on server we can use relative path to proxy service
    const url_layerTopowebb = "/proxy/topowebb";
    const url_layerFastigheter = "/proxy/fastighet";
    const url_layerOrto = "/proxy/ortofoto";

    const bounds = new Extent({
        "xmin": -964122.1691180614,
        "ymin": 6172761.97990511,
        "xmax": 1726664.7492929602,
        "ymax": 7352931.680962576,
        "spatialReference": {
            "wkid": 3006
        }
    });

    function main() {
        let layer = new WMSLayer({
            url: url_layerTopowebb,
            sublayers: [
                {
                    name: "topowebbkartan_nedtonad",
                    title: editorResources.twkTonedDownSubLayerTitle,
                    visible: false,
                    listMode: "show"
                },
                {
                    name: "topowebbkartan",
                    title: editorResources.twkSubLayerTitle,
                    visible: true,
                    listMode: "show"
                }
            ],
            "spatialReference": {
                "wkid": 3006
            },
            title: editorResources.twkLayerTitle
        });

        let layerFastigheter = new WMSLayer({
            url: url_layerFastigheter,
            sublayers: [
                {
                    name: "text",
                    title: editorResources.cadastralTextSubLayerTitle,
                    visible: true
                },
                {
                    name: "granser",
                    title: editorResources.cadastralBoundarySubLayerTitle,
                    visible: true
                }
            ],
            "spatialReference": {
                "wkid": 3006
            },
            title: editorResources.cadastralZoningLayerTitle,
            visible: false
        });

        let layerOrto = new WMSLayer({
            url: url_layerOrto,
            sublayers: [
                {
                    name: "Ortofoto_0.5",
                    title: editorResources.orthoPhoto050mSubLayerTitle,
                    visible: true
                },
                {
                    name: "Ortofoto_0.4",
                    title: editorResources.orthoPhoto040mSubLayerTitle,
                    visible: false
                },
                {
                    name: "Ortofoto_0.25",
                    title: editorResources.orthoPhoto025mSubLayerTitle,
                    visible: false
                },
                {
                    name: "Ortofoto_0.16",
                    title: editorResources.orthoPhoto016mSubLayerTitle,
                    visible: false
                },     
            ],
            "spatialReference": {
                "wkid": 3006
            },
            title: editorResources.orthoPhotoLayerTitle,
            visible: false
        });

        let map = new ArcGISMap("map", {
            scale: 13,
            zoom: 5,
            extent: bounds,
            basemap: {
                baseLayers: [layer]
            }
        });

        map.add(layer);
        map.add(layerOrto);
        map.add(layerFastigheter);

        let search, searchExpand, zoomBtn, tabulatorExpand, exifExpand;
        const boundaryLayer = new GraphicsLayer();

        let boundaryPolygon;
        let contains = true;

        const validPolygonSymbol = {
            type: "simple-fill",
            style: "solid",
            color: [0, 170, 255, 0.8],
            outline: {
              color: [0, 170, 255],
              width: 2
            }
          };

        const validPointSymbol = {
            type: "simple-marker",
            size: 6,
            color: [0, 170, 255, 0.8],
            outline: {
                width: 2,
                color: [0, 170, 255, 0.8]
            }
        };
        
        const validLineSymbol = {
            type: "simple-line",
            style: "solid",
            color: [0, 170, 255, 0.8],
            width: 5,
            outline: {
              color: [0, 170, 255],
              width: 5
            }
        };

        const invalidPointSymbol = {
            type: "simple-marker",
            size: 6,
            color: [255, 0, 0, 0.8],
            outline: {
                width: 2,
                color: [255, 0, 0, 0.8]
            }
        };

        const invalidPolygonSymbol = {
            type: "simple-fill",
            style: "diagonal-cross",
            color: [255, 0, 0],
            outline: {
                color: [255, 0, 0],
                width: 2
            }
        };

        const boundarySymbol = {
            type: "simple-fill",
            style: "solid",
            color: [0, 0, 0, 0],
            outline: {
                color: [0, 0, 0],
                width: 0
            }
        }

        map.add(boundaryLayer);
        const sketchLayer = new GraphicsLayer();
        sketchLayer.title = editorResources.drawingsLayerTitle;
        map.add(sketchLayer);
        
        const viewSpatialReference = new SpatialReference({
            wkid: 3006
        });

        let view = new MapView({
            container: "viewDiv",
            map: map,
            ui: { 
                components: []
            },
            constraints: {
                maxScale: 1000,
                lods: TileInfo.create({
                    // create the LODs to match the spatial reference of the view
                    spatialReference: viewSpatialReference
                  }).lods,
                snapToZoom: false  
            },
            extent: bounds,
            spatialReference: viewSpatialReference
        });

        let scaleBar = new ScaleBar({
            container: "scaleBarDiv",
            view: view,
            unit: "metric",
            style: "ruler"
        });
        view.ui.add(document.getElementById("scaleLabel"), "bottom-left");

        const sketchViewModel = new SketchViewModel({
            pointSymbol: validPointSymbol,
            polygonSymbol: validPolygonSymbol,
            polylineSymbol: validLineSymbol
        });

        const sketch = new Sketch({
            view: view,
            container: "sketchWidgetDiv",
            layer: sketchLayer,
            creationMode: "update",
            defaultCreateOptions: {
                mode: "click"
            },
            visibleElements: {
                settingsMenu: false,
                duplicateButton: false,
                selectionTools: {
                    "lasso-selection": false
                }
            },
            availableCreateTools: ["point", "polygon", "rectangle"], // don't use circle and polyline
            viewModel: sketchViewModel,
            
        });
        view.ui.add(document.getElementById("sketchDiv"), "top-right");
        
        //initialize attribute table
        let table = new Tabulator("#drawings-table", {
            columns: [
                { title: "id", field: "id", resizable:false, visible: false},
                { title: "Graphic", field: "graphic", resizable:false, visible: false },
                { title: editorResources.drawingsLayerTitle, field: "ritning", sorter: "string", resizable:true, variableHeight:true, formatter:cellFormatter},     
            ],
            layout:"fitDataStretch",
            layoutColumnsOnNewData:true,
            selectable:true,
            responsiveLayout:"hide"
        });

        function cellFormatter(cell, formatterParams, onRendered) {
            let geometryType = cell.getRow().getCell("graphic").getValue().geometry.type;
            let dataType = cell.getRow().getCell("graphic").getValue().attributes.DataType;
            cell.getElement().style.whiteSpace = "pre-wrap"; // To make variableHeight work. From TextArea.
            if (dataType && dataType == 4) {
                return "(" + editorResources.routeFeatureLabel + ") : " + cell.getValue();
            } else if (geometryType && geometryType === 'point') {
                return "(" + editorResources.pointFeatureLabel + ") : " + cell.getValue();
            } else if (geometryType && geometryType === 'polygon') {
                return "(" + editorResources.geoObject + ") : " + cell.getValue();
            } else {
                return cell.getValue();
            }
        }

        table.on("rowClick", function(e, row){
            let rowData = row.getData();
            if (row.isSelected()) {
                sketch.update([rowData.graphic]);
            }   
        });

        table.on("rowDblClick", function(e, row){
            view.goTo({
                target: row.getData().graphic,
                scale: 50000
            });
        });

        view.when(function () {
            //Increase size of the Sketch icons
            sketch.when(function () {
                (async () => {
                    await customElements.whenDefined("calcite-icon");
                    await document.querySelector("calcite-icon").componentOnReady();
                    $("calcite-icon[scale='s']").each(function(i,el) { 
                        $(el).attr("scale", "m");
                    });
                  })();
            });
            
            sketch.on("create", function (event) {
                if (event.state === 'start' && view.widthBreakpoint === "xsmall") {
                    document.getElementById("drawPolygonLabel").style.display = "inline";
                }
                if (event.state === 'complete') {
                    document.getElementById("drawPolygonLabel").style.display = "none";
                    const nextListId = table.getDataCount() + 1;
                    setIdentityOnGraphic(event.graphic, nextListId);
                    table.addData([{id:nextListId, ritning:event.graphic.attributes.Comment, graphic:event.graphic}], false);
                }
            });

            sketch.on("delete", function(event) {
                event.graphics.forEach(function(graphic){
                    table.deleteRow(graphic.attributes.id);
                });
              });

            sketch.on(["update", "undo", "redo"], onGraphicUpdate);
            function onGraphicUpdate(event) {
                const eventInfo = event.toolEventInfo;
                if (event.state === 'start') {
                    event.graphics.forEach(function (graphic, g) {
                        table.selectRow(graphic.attributes.id);
                    });            
                } else if (event.state === 'complete') {
                    event.graphics.forEach(function (graphic, g) {
                        table.deselectRow(graphic.attributes.id);
                        // check if the graphic is still contained by the boundary polygon as the graphic is being updated
                        contains = geometryEngine.contains(boundaryPolygon, graphic.geometry);

                        if (graphic.geometry.type === "polygon") {
                            if (!contains || graphic.geometry.rings.length != 1) { // Prevent multipolygons
                                graphic.symbol = esriLang.clone(invalidPolygonSymbol);
                            } else if (!geometryEngine.isSimple(graphic.geometry)) { // Fix intersections and duplicate koordinates by simplifying
                                let simpleGeom = tryMakeSimple(graphic.geometry);
                                if (simpleGeom === null) {
                                    graphic.symbol = esriLang.clone(invalidPolygonSymbol);
                                }
                                else {
                                    graphic.geometry = simpleGeom
                                    if (simpleGeom.rings.length != 1) { // Simplified has multipolygons
                                        graphic.symbol = esriLang.clone(invalidPolygonSymbol);
                                    } else {
                                        graphic.symbol = esriLang.clone(validPolygonSymbol);
                                    }
                                }
                            } else {
                                graphic.symbol = esriLang.clone(validPolygonSymbol);
                            }
                        }

                        if (graphic.geometry.type === "point") {
                            if (!contains) {
                                // Creates a deep clone of the graphic's symbol
                                const clonedSymbol = esriLang.clone(invalidPointSymbol);
                                graphic.symbol = clonedSymbol;
                            }
                        }
                    });
                // selecting or deselecting graphics using Shift+Left-click.
                } else if (eventInfo && eventInfo.type === "selection-change") {
                    if (eventInfo.added.length > 0) {
                        table.selectRow(eventInfo.added[0].attributes.id);
                    } else {
                        table.deselectRow(eventInfo.removed[0].attributes.id);
                    }
                }
            }

            // Get features from parent and draw on map
            if (window.opener !== null) {
                let savedGeoJsonString = $('[id*="mapsupport_"]', window.opener.document)[0].value;
                if (savedGeoJsonString !== "") {
                    getGeoJSON(savedGeoJsonString);
                }
            }

            view.popupEnabled = false; //disable popups
     
            addGraphics(); // Add the boundary polygon graphics (Sweden)

            const homeBtn = new Home({
                view: view,
                viewpoint: new Viewpoint({
                    targetGeometry: boundaryPolygon.extent,
                    scale: 6400000
                })
            });

            zoomBtn = new Zoom({
                view: view
            });

            tabulatorExpand = new Expand({
                view: view,
                expandIconClass: "esri-icon-table",
                expandTooltip: editorResources.drawingsLayerTitle,
                collapseIconClass: "esri-icon-close",
                content: document.getElementById("drawings-table")
            });

            view.ui.add(document.getElementById("actionDiv"), "top-right");
            doneButton.style.display = "inline";
            clearButton.style.display = "inline";

            view.ui.add([ // Draw polygon hint label
                    {
                        component: document.getElementById("drawPolygonLabel"),
                        position: "top-right",
                        index: 2
                    }
                ]);

            // Search widget
            search = new Search({
                view: view
            });

            // Search widget mobile
            searchExpand = new Expand({
                view: view,
                collapseIconClass: "esri-icon-close",
                content: new Search({
                    view: view,
                    container: document.createElement("div")
                })
            });

            view.ui.add(search, "top-left");

            // Add the zoom buttons to the top left corner of the view
            view.ui.add(zoomBtn, "top-left");
            // Add the home button to the top left corner of the view
            view.ui.add(homeBtn, "top-left");

            const instructionsExpand = new Expand({
                expandIconClass: "esri-icon-question",
                collapseIconClass: "esri-icon-close",
                expandTooltip: editorResources.helpTooltipLabel,
                view: view,
                expanded: true,
                mode: "floating",
                group: "top-left",
                content:
                    "<div style='width:300px; padding:10px; background-color:white'>" + editorResources.helpPart0Label
                    + "<p><p/>" + editorResources.helpPart1Label
                    + "<p><p/>" + editorResources.helpPart2Label
                    + "<p><p/>" + editorResources.helpPart3Label
                    + "<p><p/>" + editorResources.helpPart4Label
                    + "<p><p/>" + editorResources.helpPart5Label
                    + "<p><p/>" + editorResources.helpPart6Label + "</div>"
            });
            view.ui.add(instructionsExpand, "top-left");

            const fileForm = document.getElementById("exifWindow");
            exifExpand = new Expand({
                expandIconClass: "esri-icon-attachment",
                collapseIconClass: "esri-icon-close",
                expandTooltip: editorResources.exifToolTip,
                view: view,
                expanded: false,
                content: fileForm,
                group: "top-left"
            });
            view.ui.add(exifExpand, "top-left");
            document.getElementById("exifStatus").style.display = "none";
            document.getElementById("exifFailed").style.display = "none";
            document.getElementById("videoProgressDiv").style.display = "inline";

            const layerList = new LayerList({
                container: document.createElement("div"),
                view: view
            });

            const layerListExpand = new Expand({
                expandIconClass: "esri-icon-layers",  // see https://developers.arcgis.com/javascript/latest/guide/esri-icon-font/
                collapseIconClass: "esri-icon-close",
                expandTooltip: editorResources.layerListToolTip,
                view: view,
                content: layerList.domNode,
                group: "top-left"
            });

            // Add widget to the top left corner of the view
            view.ui.add(layerListExpand, "top-left");

            // Responsive layout
            view.watch("widthBreakpoint", (breakpoint) => {
                switch (breakpoint) {
                    case "xsmall":
                        updateView(true);
                        break;
                    case "small":
                    case "medium":
                    case "large":
                    case "xlarge":
                        updateView(false);
                        break;
                    default:
                }
            });

            // Close the 'help' popup when view is focused
            view.watch("focused", function (isFocused) {
                if (isFocused) {
                    instructionsExpand.expanded = false;
                }
            });

            view.watch("scale", function (newValue) {
                updateScale(parseInt(newValue), editorResources);
            });

            layerListExpand.watch("");

            view.scale = view.scale + 1; // Update scale on start so that it is initially visible
            updateScale(parseInt(view.scale), editorResources);

            const isResponsiveSize = view.widthBreakpoint === "xsmall";
            updateView(isResponsiveSize);

            GoToURLParameters(view);
        });

        view.whenLayerView(sketchLayer).then(function (layerView) {
            layerView.watch("updating", function (value) {
                if (!value) {
                    layerView
                        .queryGraphics()
                        .then(function (results) {
                            setIdentityOnGraphics(results.items);
                        });
                }
            });
        });

        document.getElementById('exifFileForm').addEventListener('change', handleFile, false);
        document.getElementById('exifDirForm').addEventListener('change', handleFile, false);
        document.getElementById('srtFileForm').addEventListener('change', handleFile, false);
        document.getElementById('srtDirForm').addEventListener('change', handleFile, false);
        
        function handleFile(event) {
            let files = event.target.files;
            let numFiles = files.length;
            document.getElementById("exifStatus").style.display = "inline";
            document.getElementById("exifFailed").style.display = 'none';
            document.getElementById('exifFilesFailed').innerHTML = '';
            document.getElementById('exifFilesTotal').innerHTML = numFiles;
            document.getElementById('exifFileSuccess').innerHTML = 0;
            let gotoCoord = false;

            for (let i = 0; i < numFiles; i++) {
                if (i == (numFiles - 1)) {
                    gotoCoord = true;
                }
                if (event.currentTarget.id == 'exifFileForm' || event.currentTarget.id == 'exifDirForm') {
                    setupExifReader(files[i], gotoCoord);
                }
                else if (event.currentTarget.id == 'srtFileForm' || event.currentTarget.id == 'srtDirForm') {
                    if (files[i].name.toLowerCase().endsWith('.srt')) {
                        setupSrtFileReader(files[i], gotoCoord);
                    }
                    else if (files[i].name.toLowerCase().endsWith('.mp4') || files[i].name.toLowerCase().endsWith('.mov') || files[i].name.toLowerCase().endsWith('.m4v')) {
                        if (NoMatchingSrtExist(files, files[i].name)) {
                            extractSrtFromVideo(files[i], gotoCoord);
                        }
                    }
                }
            }

        }

        function extractSrtFromVideo(file, gotoCoord) {
            let texttrack_id = null;
            let texttrack_timescale = null;
            let subtitleArrayClean = null;
            const samplBatchSize = 1000;
            let mp4 = MP4Box.createFile();

            mp4.onError = function (e) { console.log(e); };
            mp4.onReady = function (info) {
                for (let i = 0; i < info.tracks.length; i++) {
                    if (info.tracks[i].codec == "text") {
                        texttrack_id = info.tracks[i].id;
                        texttrack_timescale = info.tracks[i].timescale;
                    }
                }
                mp4.setExtractionOptions(texttrack_id, null, { nbSamples: samplBatchSize });
                mp4.start();
            };
            mp4.onSamples = function (id, user, samples) {
                let subtitleArray = [];
                samples.forEach(function (currentSample, index) {
                    subtitleArray.push(index + 1);
                    subtitleArray.push("".concat(SrtTime(currentSample.cts / texttrack_timescale), " --\x3e ").concat(SrtTime(currentSample.cts / texttrack_timescale + currentSample.duration / texttrack_timescale)));
                    let i = new TextDecoder("utf-8").decode(currentSample.data);
                    i = (i.slice(2, i.length)).trim();
                    let n = "";
                    for (let o = 0; o < i.length; o++)
                        0 !== i.charCodeAt(o) && (n += i[o]);
                    subtitleArray.push(n);
                    subtitleArray.push("");
                });
                mp4.releaseUsedSamples(id, samples[samples.length - 1].number);
                subtitleArrayClean = subtitleArray.join("\n");
                subtitleArrayClean = subtitleArrayClean.replace(/[\n\r]{3,}/g, "\n\n");
            };

            let offset = 0;
            if (file) {
                let videoProgressElement = createVideoProgress(1, file.name);
                let reader = file.stream().getReader();
                reader.read().then(function getNextChunk({ done, value }) {
                    if (done) {
                        mp4.flush();
                        processSrt(file, subtitleArrayClean, gotoCoord);
                        videoProgressElement.remove();
                        return;
                    }

                    videoProgressElement.value = offset / file.size;
                    let copy = value.buffer;
                    copy.fileStart = offset;
                    offset += value.length;

                    mp4.appendBuffer(copy);
                    return reader.read().then(getNextChunk);
                });
            }
            return subtitleArrayClean;
        }

        function setupExifReader(file, gotoCoord) {
            const reader = new FileReader();
            reader.onload = function (readerEvent) {
                try {
                    let tags = ExifReader.load(readerEvent.target.result, { expanded: true }); // expand to get the gps tag 
                    delete tags['MakerNote']; // Can be large so remove to lower memory usage
                    if (tags['gps']) {
                        let gpsGraphic = new Graphic({
                            geometry: {
                                type: "point",
                                latitude: tags['gps']['Latitude'],
                                longitude: tags['gps']['Longitude'],
                            },
                            symbol: validPointSymbol,
                        });
                        const outSpatialReference = new SpatialReference({
                            wkid: 3006
                        });
                        projection.load().then(function () {
                            let projGeometry = projection.project(gpsGraphic.geometry, outSpatialReference);
                            if (projGeometry != null) {
                                gpsGraphic.geometry = projGeometry;
                                gpsGraphic.setAttribute('ViewType', 'POV');
                                gpsGraphic.setAttribute('Identity', Sanitize(file.name));
                                gpsGraphic.setAttribute('Comment', Sanitize(file.name.substring(0, 79)));
                                const nextListId = table.getDataCount() + 1;
                                gpsGraphic.setAttribute('id', nextListId);
                                sketchLayer.add(gpsGraphic);                          
                                table.addData([{id:nextListId, ritning:gpsGraphic.attributes.Comment, graphic:gpsGraphic}], false);
                                document.getElementById('exifFileSuccess').innerHTML = parseInt(document.getElementById('exifFileSuccess').innerHTML) + 1
                            }
                            else {
                                gotoCoord = false;
                                if (document.getElementById("exifFailed").style.display == 'none') {
                                    document.getElementById("exifFailed").style.display = 'inline';
                                }
                                document.getElementById('exifFilesFailed').innerHTML += Sanitize(file.name) + '<br>';
                            }
                        }).then(function () { // GoTo after setting spatial ref otherwise first GoTo will goto center of map
                            if (gotoCoord) {
                                view.goTo({
                                    target: gpsGraphic,
                                    scale: 50000
                                });
                            }
                        });
                    } else {
                        if (document.getElementById("exifFailed").style.display == 'none') {
                            document.getElementById("exifFailed").style.display = 'inline';
                        }
                        document.getElementById('exifFilesFailed').innerHTML += Sanitize(file.name) + '<br>';
                    }

                } catch (error) {
                    if (document.getElementById("exifFailed").style.display == 'none') {
                        document.getElementById("exifFailed").style.display = 'inline';
                    }
                    document.getElementById('exifFilesFailed').innerHTML += Sanitize(file.name) + '<br>';
                }
            };
            reader.onerror = function (readerEvent) {
                console.debug('Error reading file');
            };

            reader.readAsArrayBuffer(file);
        }

        function setupSrtFileReader(file, gotoCoord) {
            const reader = new FileReader();
            reader.onload = function (readerEvent) {
                processSrt(file, reader.result, gotoCoord);
            };
        
            reader.onerror = function (readerEvent) {
                console.debug('Error reading file');
            };
            reader.readAsText(file);
        }

        function processSrt(file, srt, gotoCoord) {
            try {
                let coords = getSrtCoordinates(srt);
                let srtGraphic = null;

                if (coords.length == 1) {
                    srtGraphic = new Graphic({
                        geometry: {
                            type: 'point',
                            longitude: coords[0][0],
                            latitude: coords[0][1]
                        },
                        symbol: validPointSymbol,
                    });
                } else {
                    srtGraphic = new Graphic({
                        geometry: {
                            type: 'polyline',
                            paths: [coords]
                        },
                        symbol: validLineSymbol,
                    });
                }

                let outSpatialReference = new SpatialReference({
                    wkid: 3006
                });
                projection.load().then(function () {
                    let projGeometry = projection.project(srtGraphic.geometry, outSpatialReference);
                    if (projGeometry != null) {
                        srtGraphic.geometry = projGeometry;
                        srtGraphic.setAttribute('ViewType', 'POV');
                        srtGraphic.setAttribute('Identity', Sanitize(file.name));
                        srtGraphic.setAttribute('DataType', 4);
                        srtGraphic.setAttribute('Comment', Sanitize(file.name.substring(0, 79)));
                        let nextListId = table.getDataCount() + 1;
                        srtGraphic.setAttribute('id', nextListId);
                        sketchLayer.add(srtGraphic);
                        table.addData([{ id: nextListId, ritning: srtGraphic.attributes.Comment, graphic: srtGraphic }], false);
                        document.getElementById('exifFileSuccess').innerHTML = parseInt(document.getElementById('exifFileSuccess').innerHTML) + 1;
                    }
                    else {
                        gotoCoord = false;
                        if (document.getElementById("exifFailed").style.display == 'none') {
                            document.getElementById("exifFailed").style.display = 'inline';
                        }
                        document.getElementById('exifFilesFailed').innerHTML += Sanitize(file.name) + '<br>';
                    }
                }).then(function () {
                    if (gotoCoord) {
                        view.goTo({
                            target: srtGraphic,
                            scale: 50000
                        });
                    }
                });
            } catch (error) {
                if (document.getElementById("exifFailed").style.display == 'none') {
                    document.getElementById("exifFailed").style.display = 'inline';
                }
                document.getElementById('exifFilesFailed').innerHTML += Sanitize(file.name) + '<br>';
            }
        }

        function setIdentityOnGraphics(features) {
            // Set values when not set
            let allUpdatedFeatures = [];
            if (features && features.length > 0) {
                features.forEach(function (item, i) {
                    if (item.attributes == null) {
                        item.attributes = {};
                    }
                    if (item.attributes.Identity == null) {
                        item.setAttribute("Identity", "{" + uuidv4().toUpperCase() + "}");
                    }
                    if (item.attributes.Comment == null) {
                        drawingsNum += 1;
                        item.setAttribute("Comment", drawingsNum.toString());
                    }
                    if (item.popupTemplate == null) {
                        item.popupTemplate = {
                            title: "{Identity}",
                            content: [{
                                // Pass in the fields to display
                                type: "fields",
                                fieldInfos: [{
                                    fieldName: "Identity",
                                    label: "Identity"
                                }]
                            }]
                        };
                    }
                    allUpdatedFeatures.push(item);
                });
            }
            return allUpdatedFeatures;
        }

        function validatePolygonArea() {
            for (const item of sketchLayer.graphics) {
                let objectType = "";
                if (item.geometry.type == 'point') {
                    objectType = editorResources.pointFeatureLabel;
                }
                if (item.geometry.type == 'polyline') {
                    objectType = editorResources.lineFeatureLabel;
                }
                if (item.geometry.type == 'polygon') {
                    objectType = editorResources.geoObject;
                }

                const area = geometryEngine.planarArea(item.geometry, "square-kilometers");
                const contains = geometryEngine.contains(boundaryPolygon, item.geometry);

                if (!contains) { // Outside Swedens boundary
                    document.getElementById("greatPolygonModal").open = true;
                    document.getElementById("modalValidBody").innerHTML += '<br />' +
                        objectType + ": " +
                        "\xa0\xa0\xa0 " + editorResources.note + ": " + item.attributes.Comment +
                        "\xa0\xa0\xa0 " + editorResources.warningBoundary;
                }
                else if (item.geometry.type == "polygon" && item.geometry.rings.length != 1) { // Unsupported Multipolygon
                    document.getElementById("greatPolygonModal").open = true;
                    document.getElementById("modalValidBody").innerHTML += '<br />' +
                        objectType + ": " +
                        "\xa0\xa0\xa0 " + editorResources.note + ": " + item.attributes.Comment +
                        "\xa0\xa0\xa0 " + editorResources.warningMultipolygon;
                }
                else if (item.geometry.type == "polygon" && !geometryEngine.isSimple(item.geometry) && tryMakeSimple(item.geometry) == null) { // Not valid polygon
                    document.getElementById("greatPolygonModal").open = true;
                    document.getElementById("modalValidBody").innerHTML += '<br />' +
                        objectType + ": " +
                        "\xa0\xa0\xa0 " + editorResources.note + ": " + item.attributes.Comment +
                        "\xa0\xa0\xa0 " + editorResources.warningMultipolygon;
                }
                else if (area > 1) { // Area to large
                    document.getElementById("greatPolygonModal").open = true;
                    document.getElementById("modalValidBody").innerHTML += '<br />' +
                        editorResources.geoObject + ": " +
                        "\xa0\xa0\xa0 " + editorResources.note + ": " + item.attributes.Comment +
                        "\xa0\xa0\xa0 Area: " + area.toFixed(3) + " km\u00B2 " + editorResources.warningOver1km2;
                }
            }
        }

        // Save button on the map
        const doneButton = document.getElementById("btnDone");
        doneButton.addEventListener("click", function () {
            sketch.complete();
            validatePolygonArea();
            try {
                resultGeoJSON = convertToGeoJSON(sketchLayer, boundaryPolygon);
            }
            catch (error) {
                logError("Fel i konverteringen till geoJSON.", error);
                document.getElementById("errorModal").open = true;
                document.getElementById("modalErrorBody").innerHTML += '<br />' + error.stack;
                clearGraphics();
            }
            let geoJSON = JSON.parse(resultGeoJSON);
            if (geoJSON.features.length > 0) {
                document.getElementById("saveModal").open = true;
            } else {
                document.getElementById("noFeaturesModal").open = true;
            }
        });

        // Invalid geometies dialog - Clear all entries when closed
        document.getElementById("greatPolygonModal").addEventListener("calciteModalClose", function () {
            document.getElementById("modalValidBody").innerHTML = editorResources.greatPolygonModalBodyLabel;
        });

        // Invalid geometies dialog - Close button 
        document.getElementById("btnValidClose").addEventListener("click", function () {
            document.getElementById("greatPolygonModal").open = false;
        });

        // Save dialog - Save button 
        document.getElementById("btnSave").addEventListener("click", function () {
            document.getElementById("saveModal").open = false;
            postGeoJSONValue(resultGeoJSON);
        });

        // Save dialog - Cancel button 
        document.getElementById("btnCancel").addEventListener("click", function () {
            document.getElementById("saveModal").open = false;
        });

        // "Quit without any features" dialog - Close button 
        document.getElementById("btnNoneClose").addEventListener("click", function () {
            document.getElementById("noFeaturesModal").open = false;
            postGeoJSONValue("");  
        });

        // "Quit without any features" dialog - Cancel button 
        document.getElementById("btnNoneCancel").addEventListener("click", function () {
            document.getElementById("noFeaturesModal").open = false;
        });

        // Clear all button
        const clearButton = document.getElementById("btnClearAll");
        clearButton.addEventListener("click", function () {
            document.getElementById("clearAllModal").open = true;
        });

        // Clear all dialog - Yes button
        document.getElementById("btnClearYes").addEventListener("click", function () {
            clearGraphics();
            document.getElementById("exifFileForm").reset();
            document.getElementById("exifDirForm").reset();
            document.getElementById("srtFileForm").reset();
            document.getElementById("srtDirForm").reset();
            document.getElementById("clearAllModal").open = false;

            // Clean up attached exif files
            document.getElementById("exifStatus").style.display = "none";
            document.getElementById("exifFailed").style.display = "none";
        });

        // Clear all dialog - No button 
        document.getElementById("btnClearNo").addEventListener("click", function () {
            document.getElementById("clearAllModal").open = false;
        });

        // Error in converting to geoJSON
        document.getElementById("btnErrorClose").addEventListener("click", function () {
            document.getElementById("errorModal").open = false;
        });

        // Function declared globally
        getGraphics = function(item, index) {
            let jsonGeometry = Graphic.fromJSON(item);

            jsonGeometry.geometry.spatialReference = view.spatialReference;
            if (jsonGeometry.geometry.type === "point") { 
                jsonGeometry.symbol = validPointSymbol;
            } else if (jsonGeometry.geometry.type === "polygon") {
                jsonGeometry.symbol = validPolygonSymbol;
            } else if (jsonGeometry.geometry.type === "polyline") {
                jsonGeometry.symbol = validLineSymbol;
            }
            else {
                console.log("ERROR: Unsupported geometry!");
            }
            let nextListId = table.getDataCount() + 1;
            setIdentityOnGraphic(jsonGeometry, nextListId);
            table.addData([{id:nextListId, ritning:jsonGeometry.attributes.Comment, graphic:jsonGeometry}], false);
            sketchLayer.add(jsonGeometry);
        }

        // Function declared globally
        getGeoJSON = function (geoJSON) {
            let savedGeoJsonJSON = JSON.parse(geoJSON);
            let savedGeoJson = ArcgisToGeojsonUtils.geojsonToArcGIS(savedGeoJsonJSON);
            featuresJSON = savedGeoJson;

            if (savedGeoJson !== null) {
                savedGeoJson.forEach(getGraphics);
            }
        }

        // Function declared globally
        updateResultGeoJSON = function () {
            resultGeoJSON = convertToGeoJSON(sketchLayer, boundaryPolygon);
        }

        // pass geojson to listening parent
        function postGeoJSONValue(geoJSONString) {
            if (typeof geoJSONString === 'string' || geoJSONString instanceof String) {
                window.opener.postMessage(geoJSONString, window.opener.location.href);
            }
            else {
                let err = {name: "Fel vid postningen."};
                logError("geoJSON vid postning misslyckad p.g.a. att den inte är en string!", err);
            }
            
            window.onbeforeunload = null;
            window.close();
        }

        // Clears all graphics from all FeatureLayers
        function clearGraphics() {
            sketchLayer.removeAll();
            table.clearData(); // Need to clear as sketchLayer.removeAll() does not trigger the delete event
            drawingsNum = 0;
            resultGeoJSON = "";
            view.closePopup();
        }

        function addGraphics() {
            boundaryPolygon = createGeometry(swedenPolygonGeometry);
            function createGeometry(vertices) {
                return new Polygon({
                    rings: vertices,
                    spatialReference: view.spatialReference
                });
            }

            let boundaryGraphic = new Graphic({
                geometry: boundaryPolygon,
                symbol: boundarySymbol
            });
            boundaryLayer.title = editorResources.swedenPolygonBorderLabel;
            boundaryLayer.listMode = "hide";
            boundaryLayer.add(boundaryGraphic);

        }

        window.onbeforeunload = function () {
            return "Leaving this page will reset the wizard";
        };

        // Perform internationalization
        document.getElementById("btnDone").textContent = editorResources.saveLabel;
        document.getElementById("btnDone").attributes['title'].textContent = editorResources.saveLabel;
        document.getElementById("btnDone").attributes['aria-label'].textContent = editorResources.saveLabel;
        document.getElementById("btnClearAll").textContent = editorResources.clearLabel;
        document.getElementById("btnClearAll").attributes['title'].textContent = editorResources.clearLabel;
        document.getElementById("btnClearAll").attributes['aria-label'].textContent = editorResources.clearLabel;

        document.getElementById("modalSaveTitle").textContent = editorResources.saveModalTitleLabel;
        document.getElementById("modalSaveContent").textContent = editorResources.saveModalBodyLabel;
        document.getElementById("btnSave").textContent = editorResources.saveLabel;
        document.getElementById("btnCancel").textContent = editorResources.cancelLabel;


        document.getElementById("modalValidTitle").textContent = editorResources.greatPolygonModalTitleLabel;
        document.getElementById("modalValidBody").textContent = editorResources.greatPolygonModalBodyLabel;
        document.getElementById("btnValidClose").textContent = editorResources.closeLabel;

        document.getElementById("modalNoFeaturesTitle").textContent = editorResources.noFeaturesModalTitleLabel;
        document.getElementById("modalNoFeaturesContent").textContent = editorResources.noFeaturesModalBodyLabel;
        document.getElementById("btnNoneClose").textContent = editorResources.closeLabel;
        document.getElementById("btnNoneCancel").textContent = editorResources.cancelLabel;

        document.getElementById("modalClearTitle").textContent = editorResources.clearAllModalTitleLabel;
        document.getElementById("modalClearContent").textContent = editorResources.clearAllModalBodyLabel;
        document.getElementById("btnClearYes").textContent = editorResources.yesLabel;
        document.getElementById("btnClearNo").textContent = editorResources.noLabel;

        document.getElementById("modalErrorTitle").textContent = editorResources.modalErrorTitle;
        document.getElementById("modalErrorBody").textContent = editorResources.modalErrorBody;
        document.getElementById("btnErrorClose").textContent = editorResources.closeLabel;

        document.getElementById("exifDescription").textContent = editorResources.exifDescription;
        document.getElementById("exifGetFiles").textContent = editorResources.exifGetFilesLabel;
        document.getElementById("exifGetDir").textContent = editorResources.exifGetDirLabel;
        document.getElementById("exifResult").textContent = editorResources.exifResult;
        document.getElementById("exifOf").textContent = editorResources.exifOf;
        document.getElementById("exifFilesFetched").textContent = editorResources.exifFilesFetched;
        document.getElementById("exifFilesFailedLabel").textContent = editorResources.exifFilesFailedLabel;
        document.getElementById("srtGetFiles").textContent = editorResources.srtGetFilesLabel;
        document.getElementById("srtGetDir").textContent = editorResources.srtGetDirLabel;

        document.getElementById("drawPolygonLabel").textContent = editorResources.drawPolygonMobile;

        document.getElementById("sketchLabelSelect").textContent = editorResources.sketchLabelSelect;
        document.getElementById("sketchLabelDraw").textContent = editorResources.sketchLabelDraw;

        function updateScale(scale, editorResources) {
            document.getElementById("scaleLabel").innerHTML = editorResources.scaleLabel + "1:" + scale;
        }

        // Update view depending on desktop or mobile
        function updateView(isMobile) {
            if (isMobile) {
                sketch.visibleElements = {
                    undoRedoMenu: false,
                    settingsMenu: false,
                    duplicateButton: false,
                    selectionTools: {
                        "lasso-selection": false
                    }
                };

                view.ui.remove(search) // Remove the search widget and replace with expand
                view.ui.add([
                    {
                        component: searchExpand,
                        position: "top-left",
                        index: 0
                    }
                ]);

                view.ui.add([ // Remove drawings widget and replace with expand
                    {
                        component: tabulatorExpand,
                        position: "top-right",
                        index: 1
                    }
                ]);
                tabulatorExpand.visible = true; // Using visible instead of remove due to drawing-table being destoryed
                view.ui.remove(document.getElementById("drawings-table"))
                table.redraw();

                document.getElementById("drawPolygonLabel").style.display = "none"; // Hidden until drawing polygon

                document.getElementById("scaleLabel").style.display = "none";

                view.ui.remove(scaleBar); // Remove the scale bar
                
                zoomBtn.visible = false; // Hide the zoom buttons

                view.ui.move(document.getElementById("actionDiv"), "bottom-right"); // Move Save/Clear all button to bottom
            } else {
                sketch.visibleElements = {
                    undoRedoMenu: true,
                    settingsMenu: false,
                    duplicateButton: false,
                    selectionTools: {
                        "lasso-selection": false
                    }
                };

                view.ui.remove(searchExpand); // Remove search expand and add search widget
                view.ui.add([
                    {
                        component: search,
                        position: "top-left",
                        index: 0
                    }
                ]);
                
                view.ui.add([ // Remove drawings expand and replace with widget
                    {
                        component: document.getElementById("drawings-table"),
                        position: "top-right",
                        index: 1
                    }
                ]);
                tabulatorExpand.visible = false; // Using visible instead of remove due to drawing-table being destoryed
                table.redraw();

                document.getElementById("drawPolygonLabel").style.display = "none"; // Always hidden when not mobile

                document.getElementById("scaleLabel").style.width = "150px"; // Default size of the scale label
                document.getElementById("scaleLabel").style.left = "15px";
                document.getElementById("scaleLabel").style.textAlign = "center";
                document.getElementById("scaleLabel").style.display = "inline";

                view.ui.add(scaleBar, "bottom-left"); // Add scale bar

                zoomBtn.visible = true; // Show zoom buttons

                view.ui.move(document.getElementById("actionDiv"), "top-right"); //Move Save/Clear all button to top, under the drawings-table
            }
          }
        
    }

    function tryMakeSimple(geometry) { // Fix intersections and duplicate koordinates by simplifying
        let simpleGeom = geometryEngine.simplify(geometry);
        if (simpleGeom === null) {
            return null
        }
        else {
            return simpleGeom
        }
    }

    function convertToGeoJSON(sketchLayer, boundaryPolygon) {
        let geoJsonFeatures = {
                type: "FeatureCollection",
                features: []
              };
        let allGraphics = [];
        for (const item of sketchLayer.graphics) {
            if (item.geometry != null) {
                if (item.geometry.type === "polygon" && item.geometry.rings.length != 1) { // Do not allow multipolygons
                    console.log("ERROR: Multipolygon is unsupported! Removing...");
                } else if ((item.geometry.type == "polygon" && !geometryEngine.isSimple(item.geometry) && tryMakeSimple(item.geometry) == null)) { // Invalid polygon
                    console.log("ERROR: Invalid polygon detected! Removing...");
                } else if (geometryEngine.contains(boundaryPolygon, item.geometry)) { // Inside Swedens boundary so OK
                    if (!item.attributes.hasOwnProperty('DataType')) {
                        item.attributes['DataType'] = 0;
                    }
                    if (!item.attributes.hasOwnProperty('ViewType')) {
                        item.attributes['ViewType'] = 'POI';
                    }
                    allGraphics.push(item);
                }
            }
        }
        geoJsonFeatures.features = allGraphics; // mixing all geometries in one feature class
        let geoJSON = ArcgisToGeojsonUtils.arcgisToGeoJSON(geoJsonFeatures);

        //Add the coordinate reference system (crs) attribute which is not included in GeoJSON RFC 7946
        let crsObj = {
            type: "name",
            properties: {
                name: "urn:ogc:def:crs:EPSG::3006" // EPSG:3006; sweref99 tm
            }
        }

        for (let i in geoJSON.features) {
            geoJSON.features[i].geometry.coordinates = trimCoords(geoJSON.features[i].geometry.coordinates);
            delete geoJSON.features[i].properties.id; // Only used for the attribute table
        }
        geoJSON.crs = crsObj
        console.log(JSON.stringify(geoJSON));
        return JSON.stringify(geoJSON);
    }

    function trimCoords(arr) {
        for (let i = 0; i < arr.length; i++)
            if (Array.isArray(arr[i]))
                trimCoords(arr[i])
            else
                arr[i] = Math.round(arr[i] * 100) / 100;
        return arr;
    }

    function setIdentityOnGraphic(feature, listId) {
        // Set values when not set
        if (feature.attributes == null) {
            feature.attributes = {};
        }
        if (feature.attributes.Id == null) {
            feature.setAttribute("id", listId);
        }
        if (feature.attributes.Identity == null) {
            feature.setAttribute("Identity", "{" + uuidv4().toUpperCase() + "}");
        }
        if (feature.attributes.Comment == null) {
            drawingsNum += 1;
            feature.setAttribute("Comment", drawingsNum.toString());
        }
        if (feature.popupTemplate == null) {
            feature.popupTemplate = {
                title: "{Identity}",
                content: [{
                    // Pass in the fields to display
                    type: "fields",
                    fieldInfos: [{
                        fieldName: "Identity",
                        label: "Identity"
                    }]
                }]
            };
        }
    }

    function GoToURLParameters(view) {

        const params = new URLSearchParams(document.location.search);
        let northingParam = params.get("n");
        let eastingParam =  params.get("e");
        let zoomParam = params.get("z");

        if (northingParam && eastingParam) {
            let pt = new Point({
                x: eastingParam,
                y: northingParam,
                "spatialReference": {
                    "wkid": 3006
                }
            });
            let zoomLevel = 15;
            if (zoomParam) {
                zoomLevel = parseInt(zoomParam);
            }
            view.goTo({
                target: pt,
                zoom: zoomLevel
            });
        }
    }

    function Sanitize(sanitize) {
        return sanitize.replace(/[/\\?%*:|"<>]/g, '');
    }

    function createVideoProgress(id, filename) {
        const calProgress = document.createElement("calcite-progress");
        calProgress.text = filename;
        calProgress.value = 0;

        document.getElementById("videoProgressDiv").appendChild(calProgress);
        return calProgress;
    }

    function logError(message, error){
            let e = encodeURIComponent;
            let url = "kartstod.js";
            let column = 1;
            let line = 1;
            (new Image()).src = '/api/error/log2?message=' + e(message) +'&url=' + e(url) +'&line=' + e(line) +(error?.stack ? '&stack=' + e(error.stack) : '') +(column ? '&column=' + e(column) : '');
    }
});
