function getSrtCoordinates(srt_result) {
    let file_content = srt_result;
    let bracketCoords = parseBracketSrt(file_content);
    if (bracketCoords.length === 0) {
        let paranthesesCoords = parseParanthesesSrt(srt_result);
        return paranthesesCoords;
    } else {
        return bracketCoords;
    }
    
}

function parseBracketSrt(file_content) {
    const bracket_style_regex = /\[(?<key>[A-Za-z0-9_-]+)\s?:\s?(?<value>-?\d+\.\d+)\]/gmi;
    let samples = file_content.replace('\r', '').split('\n\n');
    let coordinates = [];
    samples.forEach((sample) => {
        let latitude = null;
        let longitude = null;

        for (const m of sample.matchAll(bracket_style_regex)) {
            let key = m.groups.key.toLowerCase();
            if (['latitude'].indexOf(key) != -1) {
                latitude = Number(m.groups.value).toFixed(4);
            } else if (['longitude', 'longtitude'].indexOf(key) != -1) {
                longitude = Number(m.groups.value).toFixed(4);
            }
        }

        if (!latitude && !longitude) { return };
        if (latitude == 0.0 && longitude == 0.0) { return };

        if (coordinates.length == 0) {
            coordinates.push([longitude, latitude]);
        } else {
            if (latitude == coordinates[coordinates.length - 1][1] && longitude == coordinates[coordinates.length - 1][0]) {
                return;
            } else {
                coordinates.push([longitude, latitude]);
            }
        }
    });
    return coordinates;
}

function parseParanthesesSrt(file_content) {
    const parentheses_style_regex = /(?<type>(GPS|RTK))\s*\(\s*(?<longitude>-?\d{1,3}\.\d+)[,\s]+(?<latitude>-?\d{1,3}\.\d+)/gmi;
    let samples = file_content.replace('\r', '').split('\n\n');
    let coordinates = [];
    samples.forEach((sample) => {
        let latitude = null;
        let longitude = null;

        for (const m of sample.matchAll(parentheses_style_regex)) {
            latitude = Number(m.groups.latitude).toFixed(4);
            longitude = Number(m.groups.longitude).toFixed(4);
        };

        if (!latitude && !longitude) return;
        if (latitude == 0.0 && longitude == 0.0) return;

        if (coordinates.length == 0) {
            coordinates.push([longitude, latitude]);
        } 
        else if (latitude == coordinates[coordinates.length - 1][1] && longitude == coordinates[coordinates.length - 1][0]) {
            return;
        } 
        else {
            coordinates.push([longitude, latitude]);
        }
    });
    return coordinates;
}

function NoMatchingSrtExist(files, filename) {
    let srtFile = filename.replace(/.mp4$|.mov$|.m4v$/gi, '.srt');
    let noSrtFileExist = true;
    
    for (const file of files) {
        if (file.name.toLowerCase() === srtFile.toLowerCase()) {
            noSrtFileExist = false;
        }
    }

    return noSrtFileExist;
}

function SrtTime(srtTime) {
    let time = parseInt(srtTime, 10);
    let hour = Math.floor(time / 3600);
    let min = Math.floor((time - 3600 * hour) / 60)
    let sec = time - 3600 * hour - 60 * min;
    
    return hour < 10 && (hour = "0" + hour), min < 10 && (min = "0" + min), sec < 10 && (sec = "0" + sec), hour + ":" + min + ":" + sec + ",000"
}