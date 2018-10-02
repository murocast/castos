fromCategory('subscription')
.when({
    "Castos.Events+CastosEventData+EpisodeAdded": function(s,e){
			linkTo('latest-episodes', e);
			return null;
    }
});