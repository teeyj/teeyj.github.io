// Initiate GET request (AJAX-supported)
$(document).on('click', '[data-get]', e => {
    e.preventDefault();
    const url = e.target.dataset.get;
    location = url || location;
});

// Initiate POST request (AJAX-supported)
$(document).on('click', '[data-post]', e => {
    e.preventDefault();
    const url = e.target.dataset.post;
    const f = $('<form>').appendTo(document.body)[0];
    f.method = 'post';
    f.action = url || location;
    f.submit();
});

// Trim input
$('[data-trim]').on('change', e => {
    e.target.value = e.target.value.trim();
});

// Auto uppercase
$('[data-upper]').on('input', e => {
    const a = e.target.selectionStart;
    const b = e.target.selectionEnd;
    e.target.value = e.target.value.toUpperCase();
    e.target.setSelectionRange(a, b);
});

// RESET form
$('[type=reset]').on('click', e => {
    e.preventDefault();
    location = location;
});

// Check all checkboxes
$('[data-check]').on('click', e => {
    e.preventDefault();
    const name = e.target.dataset.check;
    $(`[name=${name}]`).prop('checked', true);
});

// Uncheck all checkboxes
$('[data-uncheck]').on('click', e => {
    e.preventDefault();
    const name = e.target.dataset.uncheck;
    $(`[name=${name}]`).prop('checked', false);
});

// Row checkable (AJAX-supported)
$(document).on('click', '[data-checkable]', e => {
    if ($(e.target).is(':input,a')) return;

    $(e.currentTarget)
        .find(':checkbox')
        .prop('checked', (i, v) => !v);
});

// Photo preview
$('.upload input').on('change', e => {
    const f = e.target.files[0];
    const img = $(e.target).siblings('img')[0];

    img.dataset.src ??= img.src;

    if (f && f.type.startsWith('image/')) {
        img.onload = e => URL.revokeObjectURL(img.src);
        img.src = URL.createObjectURL(f);
    }
    else {
        img.src = img.dataset.src;
        e.target.value = '';
    }

    // Trigger input validation
    $(e.target).valid();
});

//set
$(document).on('change', 'input[name="UsageLimit-radio"]', function (e) {
    $("#UsageLimit-input").val(null);
    $("#UsageLimit-input").focus().blur();
    $("#UsageLimit-input").hide();
    if (this.value === "Limit") {
        $("#UsageLimit-input").show();
    } else if (this.value === "No Limit") {
        $("#UsageLimit-input").hide();
    }
})

$(document).on('change', 'input[name=Type]', function () {
    var $valueInput = $('#discountValue-input');
    var selected = $(this).val();

    $valueInput.rules('remove');

    if (selected === "percentage") {
        $valueInput.rules('add',
            {
                required: true,
                number: true,
                min: 1,
                max: 100,
                messages:
                {
                    required: "Please enter a percentage.",
                    number: "Please enter a valid number(1 - 100)",
                    min: "Percentage must be at least 1.",
                    max: "Percentage cannot exceed 100."
                }
            });
        $('#hint').text("Enter a percentage between 1 and 100.");
    }
    else if (selected === "fixedAmount") {
        $valueInput.rules('add',
            {
                required: true,
                number: true,
                min: 0.01,
                messages:
                {
                    required: "Please enter a amount.",
                    number: "Please enter a valid number",
                    min: "Amount must be at greater than 0.01",
                }
            });
        $('#hint').text("Enter a fixed amount greater than 0.");
    }
    $valueInput.val('');
});

$(document).on('change', 'input[name="PaymentMethod"]', function (e) {
    $("#bank").val("");
    $("#CardNumber").val(null);
    $("#TngNumber").val(null);
    $("#bank-options").hide();
    $("#bank-options").hide();
    $("#tng-method").hide();
    if (this.value === "Card") {
        $("#bank-options").show();
        $("#bank-options").show();
        $("#tng-method").hide();
    }
    else if (this.value === "TnG") {
        $("#bank-options").hide();
        $("#bank-options").hide();
        $("#tng-method").show();
    }
    else {
        $("#bank-options").hide();
        $("#bank-options").hide();
        $("#tng-method").hide();
    }
});

$(document).on('change', 'input[name="PaymentVM.PaymentMethod"]', function (e) {
    $("#bank").val("");
    $("#CardNumber").val(null);
    $("#TngNumber").val(null);
    $("#bank-options").hide();
    $("#bank-options").hide();
    $("#tng-method").hide();
    if (this.value === "Card") {
        $("#bank-options").show();
        $("#bank-options").show();
        $("#tng-method").hide();
    }
    else if (this.value === "TnG") {
        $("#bank-options").hide();
        $("#bank-options").hide();
        $("#tng-method").show();
    }
    else {
        $("#bank-options").hide();
        $("#bank-options").hide();
        $("#tng-method").hide();
    }
});

$(document).ready(function () {

    var sortableList = document.getElementById('sortable-menu');
    if (sortableList) {
        new Sortable(sortableList, {
            animation: 150,
            handle: '.menu-item, .text-muted',
            onEnd: function (evt) {
                var itemIds = $(evt.target).children().map(function () {
                    return $(this).data('id');
                }).get();

                var orderString = itemIds.join(',');

                $.ajax({
                    url: '/api/UserPreferences/SetMenuOrder',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(orderString),
                    success: function (response) {
                        console.log('Menu order saved:', orderString);
                    },
                    error: function (xhr) {
                        console.error('Failed to save menu order.');
                    }
                });
            }
        });
    }

});