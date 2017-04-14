
var xsServices = angular.module('xs.Services', []);

xsServices.service('Home', ['$http', '$q', function($http, $q)
{
	
this.Transpile = function (text)
{
	var deferred = $q.defer();

    $http.post('/' + this.__ID + '/Transpile', {
		text : text,

	}).then(function(__response) {
		deferred.resolve(__response.data.__res);
	}, function(ex){
		deferred.reject(ex);
    });

    return deferred.promise;
}


this.TranspileGraph = function (text)
{
	var deferred = $q.defer();

    $http.post('/' + this.__ID + '/TranspileGraph', {
		text : text,

	}).then(function(__response) {
		deferred.resolve(__response.data.__res);
	}, function(ex){
		deferred.reject(ex);
    });

    return deferred.promise;
}


    this.__ID = '6d7d297f-e1c9-4567-8470-dcee69792dec';
}])


