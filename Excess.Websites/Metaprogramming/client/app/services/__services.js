
            var xsServices = angular.module('xs.Services', []);
            
            xsServices.service('Home', ['$http', '$q', function($http, $q)
            {
                
            this.Transpile = function (text)
            {
                var deferred = $q.defer();

                $http.post('/' + this.__ID + '/Transpile', {
                    text : text,

                }).then(function(response) {
                    deferred.resolve(response);
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

                }).then(function(response) {
                    deferred.resolve(response);
                }, function(ex){
                    deferred.reject(ex);
                });

                return deferred.promise;
            }


                this.__ID = '5b5a77d1-fa91-42ac-b116-d545dc224185';
            }])
