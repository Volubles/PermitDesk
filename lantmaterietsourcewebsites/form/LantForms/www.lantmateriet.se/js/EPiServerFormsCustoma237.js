var RegBetValidator = function (fieldName, fieldValue, validatorMetaData) {
    // this function is used to validate visitor's data value in ViewMode
    var validateResult = { isValid: false, message: "Kunde inte nå underliggande tjänster. Försök igen senare" };
    var inputField = document.getElementsByName(fieldName);
    var guid = $(inputField[0]).attr('data-id');

    var obj = JSON.stringify({
        Name: fieldName,
        Value: fieldValue,
        Guid: guid
    });

    var serviceUrl = "/api/epi/IsValidFastighetsbeteckning/";
    $.ajax({
        url: serviceUrl,
        data: obj,
        type: 'POST',
        contentType: 'application/json',
        dataType: 'json',
        async: false,
        success: function (data, status) {
            validateResult.isValid = data;
            $(inputField[0]).val(ToTitleCase(fieldValue));
            if (!validateResult.isValid) validateResult.message = validatorMetaData.model.message;
        },
        error: function (status, error) {
            validateResult.isValid = false;
            validateResult.message = "Kunde inte nå underliggande tjänster";
        }
    });
    return validateResult;
};
var ToTitleCase = function (str) {
    str = str.toLowerCase().split(' ');
    for (var i = 0; i < str.length; i++) {
        str[i] = str[i].charAt(0).toUpperCase() + str[i].slice(1);
    }
    return str.join(' ');
}

var DuplicateValidator = function (fieldName, fieldValue, validatorMetaData) {
    //no client validation
    return { isValid: true };
};
var MaxLengthValidator = function (fieldName, fieldValue, validatorMetaData) {
    //no client validation
    return { isValid: true };
};

var LoggedinIdentityMatchFormElementValidator = function (fieldName, fieldValue, validatorMetaData) {
    // no client validation
    return { isValid: true };
};

if (typeof epi !== 'undefined' && epi.EPiServer) {
    var originalGetCustomElementValue = epi.EPiServer.Forms.Extension.getCustomElementValue;

    $.extend(true, epi.EPiServer.Forms,
    {
        /// extend the Validator to validate Visitor's value in Clientside.        
        Validators:
        {
            "Thunderstruck.Business.EpiFormsCustom.Validators.RegBetValidator": RegBetValidator,
            "Thunderstruck.Business.EpiFormsCustom.Validators.DuplicateValidator": DuplicateValidator,
            "Thunderstruck.Business.EpiFormsCustom.Validators.MaxLengthValidator": MaxLengthValidator,
            "Thunderstruck.Business.EpiFormsCustom.Validators.LoggedinIdentityMatchFormElementValidator": LoggedinIdentityMatchFormElementValidator
        },
        Extension: {
            getCustomElementValue: function ($element) {
                // if current element is not our job, let others process
                return originalGetCustomElementValue.apply(this, [$element]);
            }
        }
    });
}
