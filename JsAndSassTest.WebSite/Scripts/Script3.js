angular.module("app").factory("testyTester", function () {
    return {
        test: function() {
            console.log("tested!");
        }
    }
});