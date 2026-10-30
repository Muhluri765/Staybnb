// Staybnb Minimal Vanilla JS
document.addEventListener("DOMContentLoaded", function () {
    // Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) {
                bsAlert.close();
            }
        }, 5000);
    });

    // Dynamic Price Calculator for Details & Checkout pages
    const checkInInput = document.getElementById("calcCheckIn");
    const checkOutInput = document.getElementById("calcCheckOut");
    const pricePerNightElem = document.getElementById("dataPricePerNight");

    if (checkInInput && checkOutInput && pricePerNightElem) {
        const pricePerNight = parseFloat(pricePerNightElem.value || "0");
        const cleaningFee = 350.00;

        function updatePricing() {
            const inDate = new Date(checkInInput.value);
            const outDate = new Date(checkOutInput.value);

            if (inDate && outDate && outDate > inDate) {
                const diffTime = Math.abs(outDate - inDate);
                const nights = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

                const roomTotal = pricePerNight * nights;
                const serviceFee = Math.round(roomTotal * 0.12 * 100) / 100;
                const grandTotal = roomTotal + cleaningFee + serviceFee;

                const nightsElem = document.getElementById("calcNights");
                const roomTotalElem = document.getElementById("calcRoomTotal");
                const serviceFeeElem = document.getElementById("calcServiceFee");
                const grandTotalElem = document.getElementById("calcGrandTotal");

                if (nightsElem) nightsElem.textContent = nights.toString();
                if (roomTotalElem) roomTotalElem.textContent = "R " + roomTotal.toLocaleString("en-ZA", { minimumFractionDigits: 2 });
                if (serviceFeeElem) serviceFeeElem.textContent = "R " + serviceFee.toLocaleString("en-ZA", { minimumFractionDigits: 2 });
                if (grandTotalElem) grandTotalElem.textContent = "R " + grandTotal.toLocaleString("en-ZA", { minimumFractionDigits: 2 });
            }
        }

        checkInInput.addEventListener("change", updatePricing);
        checkOutInput.addEventListener("change", updatePricing);
        updatePricing();
    }
});
